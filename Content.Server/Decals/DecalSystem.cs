using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;

namespace Content.Server.Decals
{
    public class DecalSystem : SharedDecalSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private uint _latestIndex;
        private readonly Dictionary<GridId, HashSet<Vector2i>> _dirtyChunks = new();
        private readonly Dictionary<IPlayerSession, Dictionary<GridId, HashSet<Vector2i>>> _previousSentChunks = new();

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            MapManager.TileChanged += OnTileChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            MapManager.TileChanged -= OnTileChanged;
        }

        private void OnTileChanged(object? sender, TileChangedEventArgs e)
        {
            if (!e.NewTile.IsSpace())
                return;

            var indices = ChunkCollections[e.NewTile.GridIndex].GetIndices(e.NewTile.GridIndices);
            var toDelete = new HashSet<uint>();
            if (ChunkCollections[e.NewTile.GridIndex].TryGetChunk(indices, out var chunk))
            {
                foreach (var (uid, decal) in chunk)
                {
                    if (new Vector2((int) Math.Floor(decal.Coordinates.X), (int) Math.Floor(decal.Coordinates.Y)) ==
                        e.NewTile.GridIndices)
                    {
                        toDelete.Add(uid);
                    }
                }
            }

            if (toDelete.Count == 0) return;

            foreach (var uid in toDelete)
            {
                RemoveDecalInternal(uid);
            }

            DirtyChunk(e.NewTile.GridIndex, indices);
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            switch (e.NewStatus)
            {
                case SessionStatus.Connected:
                    _previousSentChunks[e.Session] = new();
                    break;
                case SessionStatus.Disconnected:
                    _previousSentChunks.Remove(e.Session);
                    break;
            }
        }

        protected override void DirtyChunk(GridId id, Vector2i chunkIndices)
        {
            if (!_dirtyChunks.ContainsKey(id))
            {
                _dirtyChunks[id] = new HashSet<Vector2i>();
            }

            _dirtyChunks[id].Add(chunkIndices);
        }

        public bool TryAddDecal(string id, EntityCoordinates coordinates, [NotNullWhen(true)] out uint? uid, Color? color = null, Angle? rotation = null, int zIndex = 0)
        {
            uid = 0;
            if (!PrototypeManager.HasIndex<DecalPrototype>(id))
                return false;

            var gridId = coordinates.GetGridId(EntityManager);
            if (MapManager.GetGrid(gridId).GetTileRef(coordinates).IsSpace())
                return false;

            rotation ??= Angle.Zero;
            var decal = new Decal(coordinates.Position, id, color, rotation.Value, zIndex);
            uid = _latestIndex++;
            RegisterDecal(uid.Value, decal, gridId);
            return true;
        }

        public bool RemoveDecal(uint uid) => RemoveDecalInternal(uid);

        public HashSet<uint> GetDecalsOnTile(GridId gridId, Vector2i tileIndices, Func<Decal, bool>? validDelegate = null)
        {
            var uids = new HashSet<uint>();
            var chunkIndices = ChunkCollections[gridId].GetIndices(tileIndices);
            if (!ChunkCollections[gridId].TryGetChunk(chunkIndices, out var chunk))
                return uids;

            foreach (var (uid, decal) in chunk)
            {
                if (tileIndices.X != (int) Math.Floor(decal.Coordinates.X) ||
                    tileIndices.Y != (int) Math.Floor(decal.Coordinates.Y))
                    continue;

                if (validDelegate == null || validDelegate(decal))
                {
                    uids.Add(uid);
                }
            }

            return uids;
        }

        public bool SetDecalPosition(uint uid, EntityCoordinates coordinates)
        {
            if (!ChunkIndex.TryGetValue(uid, out var values))
            {
                return false;
            }

            var gridId = coordinates.GetGridId(EntityManager);
            var decal = ChunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid];
            if (gridId == values.gridId)
            {
                ChunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid] =
                    decal.WithCoordinates(coordinates.Position);
                return true;
            }

            RemoveDecalInternal(uid);

            var chunkIndices = ChunkCollections[gridId].GetIndices(coordinates.Position);
            ChunkCollections[gridId].EnsureChunk(chunkIndices).Add(uid, decal.WithCoordinates(coordinates.Position));
            ChunkIndex[uid] = (gridId, chunkIndices);
            DirtyChunk(gridId, chunkIndices);
            return true;
        }

        public bool SetDecalColor(uint uid, Color? color)
        {
            if (!ChunkIndex.TryGetValue(uid, out var values))
            {
                return false;
            }

            var decal = ChunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid];
            ChunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid] = decal.WithColor(color);
            DirtyChunk(values);
            return true;
        }

        public bool SetDecalId(uint uid, string id)
        {
            if (!ChunkIndex.TryGetValue(uid, out var values))
            {
                return false;
            }

            if (!PrototypeManager.HasIndex<DecalPrototype>(id))
                throw new ArgumentOutOfRangeException($"Tried to set decal id to invalid prototypeid: {id}");

            var decal = ChunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid];
            ChunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid] = decal.WithId(id);
            DirtyChunk(values);
            return true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var playerSession in _playerManager.GetAllPlayers())
            {
                var chunks = GetChunksForSession(playerSession);
                var updatedChunks = new Dictionary<GridId, HashSet<Vector2i>>();
                foreach (var (gridId, gridChunks) in chunks)
                {
                    var newChunks = new HashSet<Vector2i>(gridChunks);
                    if (_previousSentChunks[playerSession].TryGetValue(gridId, out var previousChunks))
                    {
                        newChunks.ExceptWith(previousChunks);
                    }

                    if (_dirtyChunks.TryGetValue(gridId, out var dirtyChunks))
                    {
                        gridChunks.IntersectWith(dirtyChunks);
                        newChunks.UnionWith(gridChunks);
                    }

                    if (newChunks.Count == 0)
                        continue;

                    updatedChunks[gridId] = newChunks;
                }

                if(updatedChunks.Count == 0)
                    continue;

                _previousSentChunks[playerSession] = chunks;

                //send all gridChunks to client
                SendChunkUpdates(playerSession, updatedChunks);
            }

            _dirtyChunks.Clear();
        }

        private void SendChunkUpdates(IPlayerSession session, Dictionary<GridId, HashSet<Vector2i>> updatedChunks)
        {
            var updatedDecals = new Dictionary<GridId, Dictionary<Vector2i, Dictionary<uint, Decal>>>();
            foreach (var (gridId, chunks) in updatedChunks)
            {
                var gridChunks = new Dictionary<Vector2i, Dictionary<uint, Decal>>();
                foreach (var indices in chunks)
                {
                    gridChunks.Add(indices,
                        ChunkCollections[gridId].TryGetChunk(indices, out var chunk)
                            ? chunk
                            : new Dictionary<uint, Decal>());
                }
                updatedDecals[gridId] = gridChunks;
            }

            RaiseNetworkEvent(new DecalChunkUpdateEvent{Data = updatedDecals}, Filter.SinglePlayer(session));
        }

        private HashSet<EntityUid> GetSessionViewers(IPlayerSession session)
        {
            var viewers = new HashSet<EntityUid>();
            if (session.Status != SessionStatus.InGame || session.AttachedEntityUid is null)
                return viewers;

            viewers.Add(session.AttachedEntityUid.Value);

            foreach (var uid in session.ViewSubscriptions)
            {
                viewers.Add(uid);
            }

            return viewers;
        }

        private Dictionary<GridId, HashSet<Vector2i>> GetChunksForSession(IPlayerSession session)
        {
            return GetChunksForViewers(GetSessionViewers(session));
        }
    }
}
