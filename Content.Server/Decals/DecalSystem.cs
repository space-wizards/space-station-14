using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Chunking;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Content.Server.Decals
{
    public sealed class DecalSystem : SharedDecalSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
        [Dependency] private readonly IParallelManager _parMan = default!;
        [Dependency] private readonly ChunkingSystem _chunking = default!;

        private readonly Dictionary<EntityUid, HashSet<Vector2i>> _dirtyChunks = new();
        private readonly Dictionary<IPlayerSession, Dictionary<EntityUid, HashSet<Vector2i>>> _previousSentChunks = new();

        // If this ever gets parallelised then you'll want to increase the pooled count.
        private ObjectPool<HashSet<Vector2i>> _chunkIndexPool =
            new DefaultObjectPool<HashSet<Vector2i>>(
                new DefaultPooledObjectPolicy<HashSet<Vector2i>>(), 64);

        private ObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>> _chunkViewerPool =
            new DefaultObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>>(
                new DefaultPooledObjectPolicy<Dictionary<EntityUid, HashSet<Vector2i>>>(), 64);

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);

            SubscribeNetworkEvent<RequestDecalPlacementEvent>(OnDecalPlacementRequest);
            SubscribeNetworkEvent<RequestDecalRemovalEvent>(OnDecalRemovalRequest);
            SubscribeLocalEvent<PostGridSplitEvent>(OnGridSplit);
        }

        private void OnGridSplit(ref PostGridSplitEvent ev)
        {
            // Transfer decals over to the new grid.
            var enumerator = MapManager.GetGrid(ev.Grid).GetAllTilesEnumerator();
            var oldChunkCollection = DecalGridChunkCollection(ev.OldGrid);
            var chunkCollection = DecalGridChunkCollection(ev.Grid);

            if (chunkCollection == null || oldChunkCollection == null)
                return;

            while (enumerator.MoveNext(out var tile))
            {
                var tilePos = (Vector2) tile.Value.GridIndices;
                var chunkIndices = GetChunkIndices(tilePos);

                if (!oldChunkCollection.ChunkCollection.TryGetValue(chunkIndices, out var oldChunk)) continue;

                var bounds = new Box2(tilePos - 0.01f, tilePos + 1.01f);
                var toRemove = new RemQueue<uint>();

                foreach (var (oldUid, decal) in oldChunk)
                {
                    if (!bounds.Contains(decal.Coordinates)) continue;

                    var uid = chunkCollection.NextUid++;
                    var chunk = chunkCollection.ChunkCollection.GetOrNew(chunkIndices);

                    chunk[uid] = decal;
                    ChunkIndex[ev.Grid][uid] = chunkIndices;
                    DirtyChunk(ev.Grid, chunkIndices);

                    toRemove.Add(oldUid);
                    ChunkIndex[ev.OldGrid].Remove(oldUid);
                }

                foreach (var uid in toRemove)
                {
                    oldChunk.Remove(uid);
                }

                if (oldChunk.Count == 0)
                    oldChunkCollection.ChunkCollection.Remove(chunkIndices);

                if (toRemove.List?.Count > 0)
                    DirtyChunk(ev.OldGrid, chunkIndices);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        private void OnTileChanged(TileChangedEvent args)
        {
            if (!args.NewTile.IsSpace(_tileDefMan))
                return;

            var chunkCollection = ChunkCollection(args.Entity);
            if (chunkCollection == null)
                return;

            var indices = GetChunkIndices(args.NewTile.GridIndices);
            var toDelete = new HashSet<uint>();
            if (chunkCollection.TryGetValue(indices, out var chunk))
            {
                foreach (var (uid, decal) in chunk)
                {
                    if (new Vector2((int) Math.Floor(decal.Coordinates.X), (int) Math.Floor(decal.Coordinates.Y)) ==
                        args.NewTile.GridIndices)
                    {
                        toDelete.Add(uid);
                    }
                }
            }

            if (toDelete.Count == 0) return;

            foreach (var uid in toDelete)
            {
                RemoveDecalInternal( args.NewTile.GridUid, uid);
            }

            DirtyChunk(args.NewTile.GridUid, indices);
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            switch (e.NewStatus)
            {
                case SessionStatus.InGame:
                    _previousSentChunks[e.Session] = new();
                    break;
                case SessionStatus.Disconnected:
                    _previousSentChunks.Remove(e.Session);
                    break;
            }
        }

        private void OnDecalPlacementRequest(RequestDecalPlacementEvent ev, EntitySessionEventArgs eventArgs)
        {
            if (eventArgs.SenderSession is not IPlayerSession session)
                return;

            // bad
            if (!_adminManager.HasAdminFlag(session, AdminFlags.Spawn))
                return;

            if (!ev.Coordinates.IsValid(EntityManager))
                return;

            TryAddDecal(ev.Decal, ev.Coordinates, out _);
        }

        private void OnDecalRemovalRequest(RequestDecalRemovalEvent ev, EntitySessionEventArgs eventArgs)
        {
            if (eventArgs.SenderSession is not IPlayerSession session)
                return;

            // bad
            if (!_adminManager.HasAdminFlag(session, AdminFlags.Spawn))
                return;

            if (!ev.Coordinates.IsValid(EntityManager))
                return;

            var gridId = ev.Coordinates.GetGridUid(EntityManager);

            if (gridId == null)
                return;

            // remove all decals on the same tile
            foreach (var (uid, decal) in GetDecalsInRange(gridId.Value, ev.Coordinates.Position))
            {
                var chunkIndices = GetChunkIndices(decal.Coordinates);
                RemoveDecal(gridId.Value, uid);
            }
        }

        protected override void DirtyChunk(EntityUid id, Vector2i chunkIndices)
        {
            if(!_dirtyChunks.ContainsKey(id))
                _dirtyChunks[id] = new HashSet<Vector2i>();
            _dirtyChunks[id].Add(chunkIndices);
        }

        public bool TryAddDecal(string id, EntityCoordinates coordinates, [NotNullWhen(true)] out uint? uid, Color? color = null, Angle? rotation = null, int zIndex = 0, bool cleanable = false)
        {
            uid = 0;

            rotation ??= Angle.Zero;
            var decal = new Decal(coordinates.Position, id, color, rotation.Value, zIndex, cleanable);

            return TryAddDecal(decal, coordinates, out uid);
        }

        public bool TryAddDecal(Decal decal, EntityCoordinates coordinates, [NotNull] out uint? uid)
        {
            uid = 0;

            if (!PrototypeManager.HasIndex<DecalPrototype>(decal.Id))
                return false;

            var gridId = coordinates.GetGridUid(EntityManager);
            if (!MapManager.TryGetGrid(gridId, out var grid))
                return false;

            if (grid.GetTileRef(coordinates).IsSpace(_tileDefMan))
                return false;

            var chunkCollection = DecalGridChunkCollection(gridId.Value);
            if (chunkCollection == null)
                return false;

            uid = chunkCollection.NextUid++;
            var chunkIndices = GetChunkIndices(decal.Coordinates);
            if(!chunkCollection.ChunkCollection.ContainsKey(chunkIndices))
                chunkCollection.ChunkCollection[chunkIndices] = new();
            chunkCollection.ChunkCollection[chunkIndices][uid.Value] = decal;
            ChunkIndex[gridId.Value][uid.Value] = chunkIndices;
            DirtyChunk(gridId.Value, chunkIndices);

            return true;
        }

        public bool RemoveDecal(EntityUid gridId, uint uid) => RemoveDecalInternal(gridId, uid);

        public HashSet<(uint Index, Decal Decal)> GetDecalsInRange(EntityUid gridId, Vector2 position, float distance = 0.75f, Func<Decal, bool>? validDelegate = null)
        {
            var uids = new HashSet<(uint, Decal)>();
            var chunkCollection = ChunkCollection(gridId);
            var chunkIndices = GetChunkIndices(position);
            if (chunkCollection == null || !chunkCollection.TryGetValue(chunkIndices, out var chunk))
                return uids;

            foreach (var (uid, decal) in chunk)
            {
                if ((position - decal.Coordinates-new Vector2(0.5f, 0.5f)).Length > distance)
                    continue;

                if (validDelegate == null || validDelegate(decal))
                {
                    uids.Add((uid, decal));
                }
            }

            return uids;
        }

        public bool SetDecalPosition(EntityUid gridId, uint uid, EntityCoordinates coordinates)
        {
            var newGridId = coordinates.GetGridUid(EntityManager);
            if (newGridId == null)
                return false;

            return SetDecalPosition(gridId, uid, newGridId.Value, coordinates.Position);
        }

        public bool SetDecalPosition(EntityUid gridId, uint uid, EntityUid newGridId, Vector2 position)
        {
            if (!ChunkIndex.TryGetValue(gridId, out var values) || !values.TryGetValue(uid, out var indices))
            {
                return false;
            }

            DirtyChunk(gridId, indices);
            var chunkCollection = ChunkCollection(gridId);
            if (chunkCollection == null)
                return false;

            var decal = chunkCollection[indices][uid];
            if (newGridId == gridId)
            {
                chunkCollection[indices][uid] = decal.WithCoordinates(position);
                return true;
            }

            RemoveDecalInternal(gridId, uid);

            var newChunkCollection = ChunkCollection(newGridId);
            if (newChunkCollection == null)
                return false;

            var chunkIndices = GetChunkIndices(position);
            if(!newChunkCollection.ContainsKey(chunkIndices))
                newChunkCollection[chunkIndices] = new();
            newChunkCollection[chunkIndices][uid] = decal.WithCoordinates(position);
            ChunkIndex[newGridId][uid] = chunkIndices;
            DirtyChunk(newGridId, chunkIndices);
            return true;
        }

        public bool SetDecalColor(EntityUid gridId, uint uid, Color? color)
        {
            if (!ChunkIndex.TryGetValue(gridId, out var values) || !values.TryGetValue(uid, out var indices))
            {
                return false;
            }

            var chunkCollection = ChunkCollection(gridId);
            if (chunkCollection == null)
                return false;

            var chunk = chunkCollection[indices];
            var decal = chunk[uid];
            chunk[uid] = decal.WithColor(color);
            DirtyChunk(gridId, indices);
            return true;
        }

        public bool SetDecalId(EntityUid gridId, uint uid, string id)
        {
            if (!ChunkIndex.TryGetValue(gridId, out var values) || !values.TryGetValue(uid, out var indices))
            {
                return false;
            }

            if (!PrototypeManager.HasIndex<DecalPrototype>(id))
                throw new ArgumentOutOfRangeException($"Tried to set decal id to invalid prototypeid: {id}");

            var chunkCollection = ChunkCollection(gridId);
            if (chunkCollection == null)
                return false;

            var chunk = chunkCollection[indices];
            var decal = chunk[uid];
            chunk[uid] = decal.WithId(id);
            DirtyChunk(gridId, indices);
            return true;
        }

        public bool SetDecalRotation(EntityUid gridId, uint uid, Angle angle)
        {
            if (!ChunkIndex.TryGetValue(gridId, out var values) || !values.TryGetValue(uid, out var indices))
            {
                return false;
            }

            var chunkCollection = ChunkCollection(gridId);
            if (chunkCollection == null)
                return false;

            var chunk = chunkCollection[indices];
            var decal = chunk[uid];
            chunk[uid] = decal.WithRotation(angle);
            DirtyChunk(gridId, indices);
            return true;
        }

        public bool SetDecalZIndex(EntityUid gridId, uint uid, int zIndex)
        {
            if (!ChunkIndex.TryGetValue(gridId, out var values) || !values.TryGetValue(uid, out var indices))
            {
                return false;
            }

            var chunkCollection = ChunkCollection(gridId);
            if (chunkCollection == null)
                return false;

            var chunk = chunkCollection[indices];
            var decal = chunk[uid];
            chunk[uid] = decal.WithZIndex(zIndex);
            DirtyChunk(gridId, indices);
            return true;
        }

        public bool SetDecalCleanable(EntityUid gridId, uint uid, bool cleanable)
        {
            if (!ChunkIndex.TryGetValue(gridId, out var values) || !values.TryGetValue(uid, out var indices))
            {
                return false;
            }

            var chunkCollection = ChunkCollection(gridId);
            if (chunkCollection == null)
                return false;

            var chunk = chunkCollection[indices];
            var decal = chunk[uid];
            chunk[uid] = decal.WithCleanable(cleanable);
            DirtyChunk(gridId, indices);
            return true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var players = _playerManager.ServerSessions.Where(x => x.Status == SessionStatus.InGame).ToArray();
            var opts = new ParallelOptions { MaxDegreeOfParallelism = _parMan.ParallelProcessCount };
            Parallel.ForEach(players, opts, UpdatePlayer);
            _dirtyChunks.Clear();
        }

        public void UpdatePlayer(IPlayerSession player)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            var chunksInRange = _chunking.GetChunksForSession(player, ChunkSize, xformQuery, _chunkIndexPool, _chunkViewerPool);
            var staleChunks = _chunkViewerPool.Get();
            var previouslySent = _previousSentChunks[player];

            // Get any chunks not in range anymore
            // Then, remove them from previousSentChunks (for stuff like grids out of range)
            // and also mark them as stale for networking.

            foreach (var (gridId, oldIndices) in previouslySent)
            {
                // Mark the whole grid as stale and flag for removal.
                if (!chunksInRange.TryGetValue(gridId, out var chunks))
                {
                    previouslySent.Remove(gridId);

                    // Was the grid deleted?
                    if (MapManager.IsGrid(gridId))
                        staleChunks[gridId] = oldIndices;
                    else
                    {
                        // If grid was deleted then don't worry about telling the client to delete the chunk.
                        oldIndices.Clear();
                        _chunkIndexPool.Return(oldIndices);
                    }

                    continue;
                }

                var elmo = _chunkIndexPool.Get();

                // Get individual stale chunks.
                foreach (var chunk in oldIndices)
                {
                    if (chunks.Contains(chunk)) continue;
                    elmo.Add(chunk);
                }

                if (elmo.Count == 0)
                {
                    _chunkIndexPool.Return(elmo);
                    continue;
                }

                staleChunks.Add(gridId, elmo);
            }

            var updatedChunks = _chunkViewerPool.Get();
            foreach (var (gridId, gridChunks) in chunksInRange)
            {
                var newChunks = _chunkIndexPool.Get();
                _dirtyChunks.TryGetValue(gridId, out var dirtyChunks);

                if (!previouslySent.TryGetValue(gridId, out var previousChunks))
                    newChunks.UnionWith(gridChunks);
                else
                {
                    foreach (var index in gridChunks)
                    {
                        if (!previousChunks.Contains(index) || dirtyChunks != null && dirtyChunks.Contains(index))
                            newChunks.Add(index);
                    }

                    previousChunks.Clear();
                    _chunkIndexPool.Return(previousChunks);
                }

                previouslySent[gridId] = gridChunks;

                if (newChunks.Count == 0)
                    _chunkIndexPool.Return(newChunks);
                else
                    updatedChunks[gridId] = newChunks;
            }

            //send all gridChunks to client
            SendChunkUpdates(player, updatedChunks, staleChunks);
        }

        private void ReturnToPool(Dictionary<EntityUid, HashSet<Vector2i>> chunks)
        {
            foreach (var (_, previous) in chunks)
            {
                previous.Clear();
                _chunkIndexPool.Return(previous);
            }

            chunks.Clear();
            _chunkViewerPool.Return(chunks);
        }

        private void SendChunkUpdates(
            IPlayerSession session,
            Dictionary<EntityUid, HashSet<Vector2i>> updatedChunks,
            Dictionary<EntityUid, HashSet<Vector2i>> staleChunks)
        {
            var updatedDecals = new Dictionary<EntityUid, Dictionary<Vector2i, Dictionary<uint, Decal>>>();
            foreach (var (gridId, chunks) in updatedChunks)
            {
                var collection = ChunkCollection(gridId);
                if (collection == null)
                    continue;

                var gridChunks = new Dictionary<Vector2i, Dictionary<uint, Decal>>();
                foreach (var indices in chunks)
                {
                    gridChunks.Add(indices,
                        collection.TryGetValue(indices, out var chunk)
                            ? chunk
                            : new Dictionary<uint, Decal>(0));
                }
                updatedDecals[gridId] = gridChunks;
            }

            if (updatedDecals.Count != 0 || staleChunks.Count != 0)
                RaiseNetworkEvent(new DecalChunkUpdateEvent{Data = updatedDecals, RemovedChunks = staleChunks}, session);

            ReturnToPool(updatedChunks);
            ReturnToPool(staleChunks);
        }
    }
}
