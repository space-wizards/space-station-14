using System.Diagnostics.CodeAnalysis;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Decals
{
    public abstract class SharedDecalSystem : EntitySystem
    {
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] protected readonly SharedTransformSystem Transforms = default!;

        protected readonly Dictionary<GridId, Dictionary<uint, Vector2i>> ChunkIndex = new();

        public const int ChunkSize = 32;
        public static Vector2i GetChunkIndices(Vector2 coordinates) => new ((int) Math.Floor(coordinates.X / ChunkSize), (int) Math.Floor(coordinates.Y / ChunkSize));

        private float _viewSize;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
            _configurationManager.OnValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged, true);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _configurationManager.UnsubValueChanged(CVars.NetMaxUpdateRange, OnPvsRangeChanged);
        }

        private void OnPvsRangeChanged(float obj)
        {
            _viewSize = obj * 2f;
        }

        private void OnGridInitialize(GridInitializeEvent msg)
        {
            var comp = EntityManager.EnsureComponent<DecalGridComponent>(MapManager.GetGrid(msg.GridId).GridEntityId);
            ChunkIndex[msg.GridId] = new();
            foreach (var (indices, decals) in comp.ChunkCollection.ChunkCollection)
            {
                foreach (var uid in decals.Keys)
                {
                    ChunkIndex[msg.GridId][uid] = indices;
                }
            }
        }

        protected DecalGridComponent.DecalGridChunkCollection DecalGridChunkCollection(GridId gridId) => EntityManager
            .GetComponent<DecalGridComponent>(MapManager.GetGrid(gridId).GridEntityId).ChunkCollection;
        protected Dictionary<Vector2i, Dictionary<uint, Decal>> ChunkCollection(GridId gridId) => DecalGridChunkCollection(gridId).ChunkCollection;

        protected virtual void DirtyChunk(GridId id, Vector2i chunkIndices) {}

        protected bool RemoveDecalInternal(GridId gridId, uint uid)
        {
            if (!RemoveDecalHook(gridId, uid)) return false;

            if (!ChunkIndex.TryGetValue(gridId, out var values) || !values.TryGetValue(uid, out var indices))
            {
                return false;
            }

            var chunkCollection = ChunkCollection(gridId);
            if (!chunkCollection.TryGetValue(indices, out var chunk) || !chunk.Remove(uid))
            {
                return false;
            }

            if (chunkCollection[indices].Count == 0)
                chunkCollection.Remove(indices);

            ChunkIndex[gridId]?.Remove(uid);
            DirtyChunk(gridId, indices);
            return true;
        }

        protected virtual bool RemoveDecalHook(GridId gridId, uint uid) => true;

        private (Box2 view, MapId mapId) CalcViewBounds(in EntityUid euid)
        {
            var xform = EntityManager.GetComponent<TransformComponent>(euid);

            var view = Box2.UnitCentered.Scale(_viewSize).Translated(xform.WorldPosition);
            var map = xform.MapID;

            return (view, map);
        }

        protected Dictionary<GridId, HashSet<Vector2i>> GetChunksForViewers(HashSet<EntityUid> viewers)
        {
            var chunks = new Dictionary<GridId, HashSet<Vector2i>>();
            var xformQuery = GetEntityQuery<TransformComponent>();

            foreach (var viewerUid in viewers)
            {
                var (bounds, mapId) = CalcViewBounds(viewerUid);

                foreach (var grid in MapManager.FindGridsIntersecting(mapId, bounds))
                {
                    if (!chunks.ContainsKey(grid.Index))
                        chunks[grid.Index] = new HashSet<Vector2i>();

                    var enumerator = new ChunkIndicesEnumerator(Transforms.GetInvWorldMatrix(grid.GridEntityId, xformQuery).TransformBox(bounds), ChunkSize);

                    while (enumerator.MoveNext(out var indices))
                    {
                        chunks[grid.Index].Add(indices.Value);
                    }
                }
            }
            return chunks;
        }
    }

    // TODO: Pretty sure paul was moving this somewhere but just so people know
    public struct ChunkIndicesEnumerator
    {
        private Vector2i _chunkLB;
        private Vector2i _chunkRT;

        private int _xIndex;
        private int _yIndex;

        public ChunkIndicesEnumerator(Box2 localAABB, int chunkSize)
        {
            _chunkLB = new Vector2i((int)Math.Floor(localAABB.Left / chunkSize), (int)Math.Floor(localAABB.Bottom / chunkSize));
            _chunkRT = new Vector2i((int)Math.Floor(localAABB.Right / chunkSize), (int)Math.Floor(localAABB.Top / chunkSize));

            _xIndex = _chunkLB.X;
            _yIndex = _chunkLB.Y;
        }

        public bool MoveNext([NotNullWhen(true)] out Vector2i? indices)
        {
            if (_yIndex > _chunkRT.Y)
            {
                _yIndex = _chunkLB.Y;
                _xIndex += 1;
            }

            indices = new Vector2i(_xIndex, _yIndex);
            _yIndex += 1;

            return _xIndex <= _chunkRT.X;
        }
    }

    /// <summary>
    ///     Sent by clients to request that a decal is placed on the server.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestDecalPlacementEvent : EntityEventArgs
    {
        public Decal Decal;
        public EntityCoordinates Coordinates;

        public RequestDecalPlacementEvent(Decal decal, EntityCoordinates coordinates)
        {
            Decal = decal;
            Coordinates = coordinates;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RequestDecalRemovalEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates;

        public RequestDecalRemovalEvent(EntityCoordinates coordinates)
        {
            Coordinates = coordinates;
        }
    }
}
