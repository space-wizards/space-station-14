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

        public uint AddDecal(string id, EntityCoordinates coordinates, Color? color = null)
        {
            if (!_prototypeManager.HasIndex<DecalPrototype>(id))
                throw new ArgumentOutOfRangeException($"Tried to create decal with invalid prototypeid: {id}");

            var gridId = coordinates.GetGridId(EntityManager);
            var decal = new Decal(coordinates.Position, id, color);
            var uid = _latestIndex++;
            RegisterDecal(uid, decal, gridId);
            return uid;
        }

        public bool RemoveDecal(uint uid)
        {
            if (!RemoveDecalInternal(uid)) return false;

            _removedDecals.Add(uid);
            return true;
        }

        public bool SetDecalPosition(uint uid, EntityCoordinates coordinates)
        {
            if (!_chunkIndex.TryGetValue(uid, out var values))
            {
                return false;
            }

            DirtyChunk(values);

            var gridId = coordinates.GetGridId(EntityManager);
            var decal = _chunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid];
            if (gridId == values.gridId)
            {
                _chunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid] = new Decal(coordinates.Position, decal.Id, decal.Color);
                return true;
            }

            _chunkCollections[values.gridId].GetChunk(values.chunkIndices).Remove(uid);
            var chunkIndices = _chunkCollections[gridId].GetIndices(coordinates.Position);
            _chunkCollections[gridId].GetChunk(chunkIndices).Add(uid, new Decal(coordinates.Position, decal.Id, decal.Color));
            _chunkIndex[uid] = (gridId, chunkIndices);
            DirtyChunk(gridId, chunkIndices);
            return true;
        }

        public bool SetDecalColor(uint uid, Color? color)
        {
            if (!_chunkIndex.TryGetValue(uid, out var values))
            {
                return false;
            }

            var decal = _chunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid];
            _chunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid] =
                new Decal(decal.Coordinates, decal.Id, color);
            DirtyChunk(values);
            return true;
        }

        public bool SetDecalId(uint uid, string id)
        {
            if (!_chunkIndex.TryGetValue(uid, out var values))
            {
                return false;
            }

            if (!_prototypeManager.HasIndex<DecalPrototype>(id))
                throw new ArgumentOutOfRangeException($"Tried to set decal id to invalid prototypeid: {id}");

            var decal = _chunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid];
            _chunkCollections[values.gridId].GetChunk(values.chunkIndices)[uid] =
                new Decal(decal.Coordinates, id, decal.Color);
            DirtyChunk(values);
            return true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

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
                        foreach (var (uid, _) in _chunkCollections[gridId].GetChunk(chunkIndices))
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

                RaiseNetworkEvent(new DecalIndexCheckEvent{SeenIndices = seenIndices}, Filter.SinglePlayer(playerSession));
            }

            if (_removedDecals.Count > 0)
                RaiseNetworkEvent(new DecalRemovalUpdateEvent{RemovedDecals = _removedDecals});

            _dirtyChunks.Clear();
            _removedDecals.Clear();
        }

        private void SendChunkUpdates(IPlayerSession session, Dictionary<GridId, HashSet<Vector2i>> updatedChunks)
        {
            var updatedDecals = new Dictionary<uint, (Decal decal, GridId gridId)>();
            foreach (var (gridId, chunks) in updatedChunks)
            {
                foreach (var chunkIndices in chunks)
                {
                    foreach (var (uid, decal) in _chunkCollections[gridId].GetChunk(chunkIndices))
                    {
                        updatedDecals[uid] = (decal, gridId);
                    }
                }
            }
            //delta updates slap
            RaiseNetworkEvent(new DecalChunkUpdateEvent{UpdatedDecals = updatedDecals}, Filter.SinglePlayer(session));
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
