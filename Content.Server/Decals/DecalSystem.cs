using System;
using System.Collections.Generic;
using Content.Shared.Decals;
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
        private readonly HashSet<uint> _removedDecals = new();
        private readonly Dictionary<IPlayerSession, Dictionary<GridId, HashSet<Vector2i>>> _previousSentChunks = new();

        protected override void DirtyChunk(GridId id, Vector2i chunkIndices)
        {
            if (!_dirtyChunks.ContainsKey(id))
            {
                _dirtyChunks[id] = new HashSet<Vector2i>();
            }

            _dirtyChunks[id].Add(chunkIndices);
        }

        public uint AddDecal(string id, GridId gridId, Vector2 coordinates, Color? color = null)
        {
            var decal = new Decal(coordinates, id, color);
            var uid = _latestIndex++;
            RegisterDecal(uid, decal, gridId);
            return uid;
        }

        public uint AddDecal(string id, EntityCoordinates coordinates, Color? color = null)
        {
            if (!PrototypeManager.HasIndex<DecalPrototype>(id))
                throw new ArgumentOutOfRangeException($"Tried to create decal with invalid prototypeid: {id}");

            var gridId = coordinates.GetGridId(EntityManager);
            return AddDecal(id, gridId, coordinates.Position, color);
        }

        public bool RemoveDecal(uint uid)
        {
            if (!RemoveDecalInternal(uid)) return false;

            _removedDecals.Add(uid);
            return true;
        }

        public HashSet<uint> GetDecalsOnTile(GridId gridId, Vector2i tileIndices, Func<Decal, bool>? validDelegate = null)
        {
            var uids = new HashSet<uint>();
            var chunkIndices = ChunkCollections[gridId].GetIndices(tileIndices);
            foreach (var (uid, decal) in ChunkCollections[gridId].GetChunk(chunkIndices))
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

            DirtyChunk(values);

            var gridId = coordinates.GetGridId(EntityManager);
            var decal = ChunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid];
            if (gridId == values.gridId)
            {
                ChunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid] = new Decal(coordinates.Position, decal.Id, decal.Color);
                return true;
            }

            ChunkCollections[values.gridId].GetChunk(values.chunkIndices).Remove(uid);
            var chunkIndices = ChunkCollections[gridId].GetIndices(coordinates.Position);
            ChunkCollections[gridId].GetChunk(chunkIndices).Add(uid, new Decal(coordinates.Position, decal.Id, decal.Color));
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
            ChunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid] =
                new Decal(decal.Coordinates, decal.Id, color);
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
            ChunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid] =
                new Decal(decal.Coordinates, id, decal.Color);
            DirtyChunk(values);
            return true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_removedDecals.Count > 0)
                RaiseNetworkEvent(new DecalRemovalUpdateEvent{RemovedDecals = _removedDecals});

            foreach (var playerSession in _playerManager.GetAllPlayers())
            {
                if (!_previousSentChunks.TryGetValue(playerSession, out var previous))
                    previous = new();

                var seenIndices = new HashSet<uint>();
                var chunks = GetChunksForSession(playerSession);
                var updatedChunks = new Dictionary<GridId, HashSet<Vector2i>>();
                foreach (var (gridId, gridChunks) in chunks)
                {
                    foreach (var chunkIndices in gridChunks)
                    {
                        foreach (var (uid, _) in ChunkCollections[gridId].GetChunk(chunkIndices))
                        {
                            seenIndices.Add(uid);
                        }
                    }

                    var newChunks = new HashSet<Vector2i>(gridChunks);
                    if (previous.TryGetValue(gridId, out var previousChunks))
                    {
                        newChunks.ExceptWith(previousChunks);
                    }

                    if(!_dirtyChunks.TryGetValue(gridId, out var dirtyChunks))
                        continue;

                    gridChunks.IntersectWith(dirtyChunks);
                    gridChunks.UnionWith(newChunks);

                    if (gridChunks.Count == 0)
                        continue;

                    updatedChunks[gridId] = gridChunks;
                }

                _previousSentChunks[playerSession] = updatedChunks;

                //send all gridChunks to client
                if(updatedChunks.Count != 0)
                    SendChunkUpdates(playerSession, updatedChunks);

                //todo paul this is purely to check that i havent fucked up my logic, should be removed once the system is proven to work
                RaiseNetworkEvent(new DecalIndexCheckEvent{SeenIndices = seenIndices}, Filter.SinglePlayer(playerSession));
            }

            _dirtyChunks.Clear();
            _removedDecals.Clear();
        }

        private void SendChunkUpdates(IPlayerSession session, Dictionary<GridId, HashSet<Vector2i>> updatedChunks)
        {
            var updatedDecals = new Dictionary<GridId, Dictionary<Vector2i, Dictionary<uint, Decal>>>();
            foreach (var (gridId, chunks) in updatedChunks)
            {
                var gridChunks = new Dictionary<Vector2i, Dictionary<uint, Decal>>();
                foreach (var indices in chunks)
                {
                    gridChunks.Add(indices, ChunkCollections[gridId].GetChunk(indices));
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
