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
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Server.Decals
{
    public sealed class DecalSystem : SharedDecalSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
        [Dependency] private readonly IParallelManager _parMan = default!;
        [Dependency] private readonly ChunkingSystem _chunking = default!;
        [Dependency] private readonly IConfigurationManager _conf = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private readonly Dictionary<EntityUid, HashSet<Vector2i>> _dirtyChunks = new();
        private readonly Dictionary<IPlayerSession, Dictionary<EntityUid, HashSet<Vector2i>>> _previousSentChunks = new();

        // If this ever gets parallelised then you'll want to increase the pooled count.
        private ObjectPool<HashSet<Vector2i>> _chunkIndexPool =
            new DefaultObjectPool<HashSet<Vector2i>>(
                new DefaultPooledObjectPolicy<HashSet<Vector2i>>(), 64);

        private ObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>> _chunkViewerPool =
            new DefaultObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>>(
                new DefaultPooledObjectPolicy<Dictionary<EntityUid, HashSet<Vector2i>>>(), 64);
        private bool _pvsEnabled;

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
            SubscribeLocalEvent<DecalGridComponent, ComponentGetState>(OnGetState);

            SubscribeNetworkEvent<RequestDecalPlacementEvent>(OnDecalPlacementRequest);
            SubscribeNetworkEvent<RequestDecalRemovalEvent>(OnDecalRemovalRequest);
            SubscribeLocalEvent<PostGridSplitEvent>(OnGridSplit);

            _conf.OnValueChanged(CVars.NetPVS, OnPvsToggle, true);
        }

        private void OnPvsToggle(bool value)
        {
            if (value == _pvsEnabled)
                return;

            _pvsEnabled = value;

            if (value)
                return;

            foreach (var playerData in _previousSentChunks.Values)
            {
                playerData.Clear();
            }

            foreach (var (grid, meta) in EntityQuery<DecalGridComponent, MetaDataComponent>(true))
            {
                grid.ForceTick = _timing.CurTick;
                Dirty(grid, meta);
            }
        }

        private void OnGetState(EntityUid uid, DecalGridComponent component, ref ComponentGetState args)
        {
            if (_pvsEnabled && !args.ReplayState)
                return;

            // Should this be a full component state or a delta-state?
            if (args.FromTick <= component.CreationTick || args.FromTick <= component.ForceTick)
            {
                args.State = new DecalGridState(component.ChunkCollection.ChunkCollection);
                return;
            }

            var data = new Dictionary<Vector2i, DecalChunk>();
            foreach (var (index, chunk) in component.ChunkCollection.ChunkCollection)
            {
                if (chunk.LastModified >= args.FromTick)
                    data[index] = chunk;
            }

            args.State = new DecalGridState(data) { AllChunks = new(component.ChunkCollection.ChunkCollection.Keys) };
        }

        private void OnGridSplit(ref PostGridSplitEvent ev)
        {
            if (!TryComp(ev.OldGrid, out DecalGridComponent? oldComp))
                return;

            if (!TryComp(ev.Grid, out DecalGridComponent? newComp))
                return;

            // Transfer decals over to the new grid.
            var enumerator = MapManager.GetGrid(ev.Grid).GetAllTilesEnumerator();

            var oldChunkCollection = oldComp.ChunkCollection.ChunkCollection;
            var chunkCollection = newComp.ChunkCollection.ChunkCollection;

            while (enumerator.MoveNext(out var tile))
            {
                var tilePos = (Vector2) tile.Value.GridIndices;
                var chunkIndices = GetChunkIndices(tilePos);

                if (!oldChunkCollection.TryGetValue(chunkIndices, out var oldChunk))
                    continue;

                var bounds = new Box2(tilePos - 0.01f, tilePos + 1.01f);
                var toRemove = new RemQueue<uint>();

                foreach (var (oldDecalId, decal) in oldChunk.Decals)
                {
                    if (!bounds.Contains(decal.Coordinates))
                        continue;

                    var newDecalId = newComp.ChunkCollection.NextDecalId++;
                    var newChunk = chunkCollection.GetOrNew(chunkIndices);
                    newChunk.Decals[newDecalId] = decal;
                    newComp.DecalIndex[newDecalId] = chunkIndices;
                    toRemove.Add(oldDecalId);
                }

                foreach (var oldDecalId in toRemove)
                {
                    oldChunk.Decals.Remove(oldDecalId);
                    oldComp.DecalIndex.Remove(oldDecalId);
                }

                DirtyChunk(ev.Grid, chunkIndices, chunkCollection.GetOrNew(chunkIndices));

                if (oldChunk.Decals.Count == 0)
                    oldChunkCollection.Remove(chunkIndices);

                if (toRemove.List?.Count > 0)
                    DirtyChunk(ev.OldGrid, chunkIndices, oldChunk);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _conf.UnsubValueChanged(CVars.NetPVS, OnPvsToggle);
        }

        private void OnTileChanged(ref TileChangedEvent args)
        {
            if (!args.NewTile.IsSpace(_tileDefMan))
                return;

            if (!TryComp(args.Entity, out DecalGridComponent? grid))
                return;

            var indices = GetChunkIndices(args.NewTile.GridIndices);
            var toDelete = new HashSet<uint>();
            if (!grid.ChunkCollection.ChunkCollection.TryGetValue(indices, out var chunk))
                return;

            foreach (var (uid, decal) in chunk.Decals)
            {
                if (new Vector2((int) Math.Floor(decal.Coordinates.X), (int) Math.Floor(decal.Coordinates.Y)) ==
                    args.NewTile.GridIndices)
                {
                    toDelete.Add(uid);
                }
            }

            if (toDelete.Count == 0)
                return;

            foreach (var decalId in toDelete)
            {
                grid.DecalIndex.Remove(decalId);
                chunk.Decals.Remove(decalId);
            }

            DirtyChunk(args.Entity, indices, chunk);
            if (chunk.Decals.Count == 0)
                grid.ChunkCollection.ChunkCollection.Remove(indices);
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
            foreach (var (decalId, _) in GetDecalsInRange(gridId.Value, ev.Coordinates.Position))
            {
                RemoveDecal(gridId.Value, decalId);
            }
        }

        protected override void DirtyChunk(EntityUid id, Vector2i chunkIndices, DecalChunk chunk)
        {
            chunk.LastModified = _timing.CurTick;
            if(!_dirtyChunks.ContainsKey(id))
                _dirtyChunks[id] = new HashSet<Vector2i>();
            _dirtyChunks[id].Add(chunkIndices);
        }

        public bool TryAddDecal(string id, EntityCoordinates coordinates, out uint decalId, Color? color = null, Angle? rotation = null, int zIndex = 0, bool cleanable = false)
        {
            rotation ??= Angle.Zero;
            var decal = new Decal(coordinates.Position, id, color, rotation.Value, zIndex, cleanable);

            return TryAddDecal(decal, coordinates, out decalId);
        }

        public bool TryAddDecal(Decal decal, EntityCoordinates coordinates, out uint decalId)
        {
            decalId = 0;

            if (!PrototypeManager.HasIndex<DecalPrototype>(decal.Id))
                return false;

            var gridId = coordinates.GetGridUid(EntityManager);
            if (!MapManager.TryGetGrid(gridId, out var grid))
                return false;

            if (grid.GetTileRef(coordinates).IsSpace(_tileDefMan))
                return false;

            if (!TryComp(gridId, out DecalGridComponent? comp))
                return false;

            decalId = comp.ChunkCollection.NextDecalId++;
            var chunkIndices = GetChunkIndices(decal.Coordinates);
            var chunk = comp.ChunkCollection.ChunkCollection.GetOrNew(chunkIndices);
            chunk.Decals[decalId] = decal;
            comp.DecalIndex[decalId] = chunkIndices;
            DirtyChunk(gridId.Value, chunkIndices, chunk);

            return true;
        }

        public bool RemoveDecal(EntityUid gridId, uint decalId, DecalGridComponent? component = null)
            => RemoveDecalInternal(gridId, decalId, out _, component);

        public HashSet<(uint Index, Decal Decal)> GetDecalsInRange(EntityUid gridId, Vector2 position, float distance = 0.75f, Func<Decal, bool>? validDelegate = null)
        {
            var decalIds = new HashSet<(uint, Decal)>();
            var chunkCollection = ChunkCollection(gridId);
            var chunkIndices = GetChunkIndices(position);
            if (chunkCollection == null || !chunkCollection.TryGetValue(chunkIndices, out var chunk))
                return decalIds;

            foreach (var (uid, decal) in chunk.Decals)
            {
                if ((position - decal.Coordinates-new Vector2(0.5f, 0.5f)).Length > distance)
                    continue;

                if (validDelegate == null || validDelegate(decal))
                {
                    decalIds.Add((uid, decal));
                }
            }

            return decalIds;
        }

        /// <summary>
        ///     Changes a decals position. Note this will actually result in a new decal being created, possibly on a new grid or chunk.
        /// </summary>
        /// <remarks>
        ///     If the new position is invalid, this will result in the decal getting deleted.
        /// </remarks>
        public bool SetDecalPosition(EntityUid gridId, uint decalId, EntityCoordinates coordinates, DecalGridComponent? comp = null)
        {
            if (!Resolve(gridId, ref comp))
                return false;

            if (!RemoveDecalInternal(gridId, decalId, out var removed, comp))
                return false;

            return TryAddDecal(removed.WithCoordinates(coordinates.Position), coordinates, out _);
        }

        private bool ModifyDecal(EntityUid gridId, uint decalId, Func<Decal, Decal> modifyDecal, DecalGridComponent? comp = null)
        {
            if (!Resolve(gridId, ref comp))
                return false;

            if (!comp.DecalIndex.TryGetValue(decalId, out var indices))
                return false;

            var chunk = comp.ChunkCollection.ChunkCollection[indices];
            var decal = chunk.Decals[decalId];
            chunk.Decals[decalId] = modifyDecal(decal);
            DirtyChunk(gridId, indices, chunk);
            return true;
        }

        public bool SetDecalColor(EntityUid gridId, uint decalId, Color? value, DecalGridComponent? comp = null)
            => ModifyDecal(gridId, decalId, x => x.WithColor(value), comp);

        public bool SetDecalRotation(EntityUid gridId, uint decalId, Angle value, DecalGridComponent? comp = null)
            => ModifyDecal(gridId, decalId, x => x.WithRotation(value), comp);

        public bool SetDecalZIndex(EntityUid gridId, uint decalId, int value, DecalGridComponent? comp = null)
            => ModifyDecal(gridId, decalId, x => x.WithZIndex(value), comp);

        public bool SetDecalCleanable(EntityUid gridId, uint decalId, bool value, DecalGridComponent? comp = null)
            => ModifyDecal(gridId, decalId, x => x.WithCleanable(value), comp);

        public bool SetDecalId(EntityUid gridId, uint decalId, string id, DecalGridComponent? comp = null)
        {
            if (!PrototypeManager.HasIndex<DecalPrototype>(id))
                throw new ArgumentOutOfRangeException($"Tried to set decal id to invalid prototypeid: {id}");

            return ModifyDecal(gridId, decalId, x => x.WithId(id), comp);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var ent in _dirtyChunks.Keys)
            {
                if (TryComp(ent, out DecalGridComponent? decals))
                    Dirty(decals);
            }

            if (!_pvsEnabled)
            {
                _dirtyChunks.Clear();
                return;
            }

            if (_pvsEnabled)
            {
                var players = _playerManager.ServerSessions.Where(x => x.Status == SessionStatus.InGame).ToArray();
                var opts = new ParallelOptions { MaxDegreeOfParallelism = _parMan.ParallelProcessCount };
                Parallel.ForEach(players, opts, UpdatePlayer);
            }

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
            var updatedDecals = new Dictionary<EntityUid, Dictionary<Vector2i, DecalChunk>>();
            foreach (var (gridId, chunks) in updatedChunks)
            {
                var collection = ChunkCollection(gridId);
                if (collection == null)
                    continue;

                var gridChunks = new Dictionary<Vector2i, DecalChunk>();
                foreach (var indices in chunks)
                {
                    gridChunks.Add(indices,
                        collection.TryGetValue(indices, out var chunk)
                            ? chunk
                            : new());
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
