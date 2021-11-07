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

            var chunkCollection = ChunkCollection(e.NewTile.GridIndex);
            var indices = GetChunkIndices(e.NewTile.GridIndices);
            var toDelete = new HashSet<uint>();
            if (chunkCollection.TryGetValue(indices, out var chunk))
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
                RemoveDecalInternal(e.NewTile.GridIndex, uid);
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
            if(!_dirtyChunks.ContainsKey(id))
                _dirtyChunks[id] = new HashSet<Vector2i>();
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
            var chunkCollection = DecalGridChunkCollection(gridId);
            uid = chunkCollection.NextUid++;
            var chunkIndices = GetChunkIndices(decal.Coordinates);
            if(!chunkCollection.ChunkCollection.ContainsKey(chunkIndices))
                chunkCollection.ChunkCollection[chunkIndices] = new();
            chunkCollection.ChunkCollection[chunkIndices][uid.Value] = decal;
            ChunkIndex[gridId][uid.Value] = chunkIndices;
            DirtyChunk(gridId, chunkIndices);
            return true;
        }

        public bool RemoveDecal(GridId gridId, uint uid) => RemoveDecalInternal(gridId, uid);

        public HashSet<uint> GetDecalsOnTile(GridId gridId, Vector2i tileIndices, Func<Decal, bool>? validDelegate = null)
        {
            var uids = new HashSet<uint>();
            var chunkCollection = ChunkCollection(gridId);
            var chunkIndices = GetChunkIndices(tileIndices);
            if (!chunkCollection.TryGetValue(chunkIndices, out var chunk))
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

        public bool SetDecalPosition(GridId gridId, uint uid, EntityCoordinates coordinates)
        {
            if (!ChunkIndex.TryGetValue(gridId, out var values) || !values.TryGetValue(uid, out var indices))
            {
                return false;
            }

            var newGridId = coordinates.GetGridId(EntityManager);
            var chunkCollection = ChunkCollection(gridId);
            var decal = chunkCollection[indices][uid];
            if (newGridId == gridId)
            {
                chunkCollection[indices][uid] =
                    decal.WithCoordinates(coordinates.Position);
                return true;
            }

            RemoveDecalInternal(gridId, uid);

            var newChunkCollection = ChunkCollection(newGridId);
            var chunkIndices = GetChunkIndices(coordinates.Position);
            if(!newChunkCollection.ContainsKey(chunkIndices))
                newChunkCollection[chunkIndices] = new();
            newChunkCollection[chunkIndices][uid] = decal.WithCoordinates(coordinates.Position);
            ChunkIndex[newGridId][uid] = chunkIndices;
            DirtyChunk(newGridId, chunkIndices);
            return true;
        }

        public bool SetDecalColor(GridId gridId, uint uid, Color? color)
        {
            if (!ChunkIndex.TryGetValue(gridId, out var values) || !values.TryGetValue(uid, out var indices))
            {
                return false;
            }

            var chunkCollection = ChunkCollection(gridId);
            var decal = chunkCollection[indices][uid];
            chunkCollection[indices][uid] = decal.WithColor(color);
            DirtyChunk(gridId, indices);
            return true;
        }

        public bool SetDecalId(GridId gridId, uint uid, string id)
        {
            if (!ChunkIndex.TryGetValue(gridId, out var values) || !values.TryGetValue(uid, out var indices))
            {
                return false;
            }

            if (!PrototypeManager.HasIndex<DecalPrototype>(id))
                throw new ArgumentOutOfRangeException($"Tried to set decal id to invalid prototypeid: {id}");

            var chunkCollection = ChunkCollection(gridId);
            var decal = chunkCollection[indices][uid];
            chunkCollection[indices][uid] = decal.WithId(id);
            DirtyChunk(gridId, indices);
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
                        ChunkCollection(gridId).TryGetValue(indices, out var chunk)
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
