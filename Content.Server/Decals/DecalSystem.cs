using System;
using System.Collections.Generic;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Decals
{
    public class DecalSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerGameStateManager _serverGameStateManager = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;

        private Dictionary<GridId, ChunkCollection<List<Decal>>> _chunkCollections = new();

        public record Decal(Vector2 Coordinates, string Id, Color? Color);

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
            _chunkCollections[msg.GridId] = new ChunkCollection<List<Decal>>(new Vector2i(32, 32));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var playerSession in _playerManager.GetAllPlayers())
            {
                //hi yes i'd like line 240 to 247 from pvs please
                var (box, mapid) = CalcViewBounds()
                foreach (var VARIABLE in )
                {

                }
            }
        }

        // copied from pvssystem. do not merge if still here | Read Safe
        private (Box2 view, MapId mapId) CalcViewBounds(in EntityUid euid)
        {
            var xform = EntityManager.GetComponent<ITransformComponent>(euid);

            var view = Box2.UnitCentered.Scale(_serverGameStateManager.PvsRange*2f).Translated(xform.WorldPosition);
            var map = xform.MapID;

            return (view, map);
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

        private Vector2i ExtractIndices(Vector2i a)
        {
            return new ((int) Math.Floor((double) a.X / _chunkSize.X), (int) Math.Floor((double) a.Y / _chunkSize.Y));
        }

        public T GetChunk(Vector2i indices)
        {
            if (_chunks.TryGetValue(indices, out var chunk))
                return chunk;

            return _chunks[indices] = new T();
        }

        public T GetChunkForPoint(Vector2i point)
        {
            return GetChunk(ExtractIndices(point));
        }

        public IEnumerable<T> GetChunksForArea(Box2i area)
        {
            var coordinates = new HashSet<Vector2i>();

            var bottomRight = ExtractIndices(area.BottomRight);
            var topLeft = ExtractIndices(area.TopLeft);

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
