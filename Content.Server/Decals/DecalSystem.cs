using System;
using System.Collections.Generic;
using Content.Shared.Decals;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Decals
{
    public class DecalSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerGameStateManager _serverGameStateManager = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;

        private readonly Dictionary<GridId, ChunkCollection<Dictionary<uint, Decal>>> _chunkCollections = new();
        private readonly Dictionary<uint, (GridId gridId, Vector2i chunkIndices)> _chunkIndex = new();
        private uint _latestIndex;
        private readonly Dictionary<GridId, HashSet<Vector2i>> _dirtyChunks = new();
        private readonly Dictionary<IPlayerSession, Dictionary<GridId, HashSet<Vector2i>>> _previousSentChunks = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
        }

        private void OnGridRemoval(GridRemovalEvent msg)
        {
            _chunkCollections.Remove(msg.GridId);
        }

        private void OnGridInitialize(GridInitializeEvent msg)
        {
            _chunkCollections[msg.GridId] = new ChunkCollection<Dictionary<uint, Decal>>(new Vector2i(32, 32));
        }

        private void DirtyChunk((GridId, Vector2i) values) => DirtyChunk(values.Item1, values.Item2);
        private void DirtyChunk(GridId id, Vector2i chunkIndices)
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
            var indices = _chunkCollections[gridId].GetIndices(coordinates.Position);
            _chunkCollections[gridId].GetChunk(indices).Add(uid, decal);
            _chunkIndex.Add(uid, (gridId, indices));
            DirtyChunk(gridId, indices);
            return uid;
        }

        public bool RemoveDecal(uint uid)
        {
            if (!_chunkIndex.TryGetValue(uid, out var values))
            {
                return false;
            }

            if (!_chunkCollections[values.gridId].GetChunk(values.chunkIndices).Remove(uid))
            {
                return false;
            }

            _chunkIndex.Remove(uid);
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

                var chunks = GetChunksForSession(playerSession);
                var updatedChunks = new Dictionary<GridId, HashSet<Vector2i>>();
                foreach (var (gridId, gridChunks) in chunks)
                {
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

                //send all gridChunks to client
                if(updatedChunks.Count != 0)
                    SendChunkUpdates(playerSession, updatedChunks);
            }

            _dirtyChunks.Clear();
        }

        private Dictionary<GridId, HashSet<Vector2i>> GetChunksForSession(IPlayerSession session)
        {
            //haha lets just copy some pvs code
            //this should probably be in shared so the client can do some culling
        }

        private void SendChunkUpdates(IPlayerSession session, Dictionary<GridId, HashSet<Vector2i>> updatedChunks)
        {
            //delta updates slap
            RaiseNetworkEvent(new DecalChunkUpdateEvent{UpdatedChunks = updatedChunks}, Filter.SinglePlayer(session));
        }
    }

    public class ChunkCollection<T> where T : new()
    {
        private readonly Dictionary<Vector2i, T> _chunks = new();
        private readonly Vector2i _chunkSize;

        public ChunkCollection(Vector2i chunkSize)
        {
            _chunkSize = chunkSize;
        }

        public Vector2i GetIndices(Vector2 a)
        {
            return new ((int) Math.Floor(a.X / _chunkSize.X), (int) Math.Floor(a.Y / _chunkSize.Y));
        }

        public T GetChunk(Vector2i indices)
        {
            if (_chunks.TryGetValue(indices, out var chunk))
                return chunk;

            return _chunks[indices] = new T();
        }

        public IEnumerable<T> GetChunksForArea(Box2 area)
        {
            var coordinates = new HashSet<Vector2i>();

            var bottomRight = GetIndices(area.BottomRight);
            var topLeft = GetIndices(area.TopLeft);

            for (var x = 0; x < bottomRight.X - topLeft.X; x++)
            {
                for (var y = 0; y < topLeft.Y - bottomRight.Y; y++)
                {
                    coordinates.Add(new Vector2i(x, y));
                }
            }

            foreach (var indices in coordinates)
            {
                yield return GetChunk(indices);
            }
        }
    }
}
