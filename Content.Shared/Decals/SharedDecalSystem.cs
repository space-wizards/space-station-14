using System.Diagnostics.CodeAnalysis;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Shared.Decals
{
    public abstract class SharedDecalSystem : EntitySystem
    {
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;

        protected readonly Dictionary<EntityUid, Dictionary<uint, Vector2i>> ChunkIndex = new();

        // Note that this constant is effectively baked into all map files, because of how they save the grid decal component.
        // So if this ever needs changing, the maps need converting.
        public const int ChunkSize = 32;
        public static Vector2i GetChunkIndices(Vector2 coordinates) => new ((int) Math.Floor(coordinates.X / ChunkSize), (int) Math.Floor(coordinates.Y / ChunkSize));

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
        }

        private void OnGridInitialize(GridInitializeEvent msg)
        {
            var comp = EntityManager.EnsureComponent<DecalGridComponent>(msg.EntityUid);
            ChunkIndex[msg.EntityUid] = new();
            foreach (var (indices, decals) in comp.ChunkCollection.ChunkCollection)
            {
                foreach (var uid in decals.Decals.Keys)
                {
                    ChunkIndex[msg.EntityUid][uid] = indices;
                }
            }
        }

        protected DecalGridComponent.DecalGridChunkCollection? DecalGridChunkCollection(EntityUid gridEuid, DecalGridComponent? comp = null)
        {
            if (!Resolve(gridEuid, ref comp))
                return null;

            return comp.ChunkCollection;
        }

        protected Dictionary<Vector2i, DecalChunk>? ChunkCollection(EntityUid gridEuid, DecalGridComponent? comp = null)
        {
            var collection = DecalGridChunkCollection(gridEuid, comp);
            return collection?.ChunkCollection;
        }

        protected virtual void DirtyChunk(EntityUid id, Vector2i chunkIndices, DecalChunk chunk) {}

        protected bool RemoveDecalInternal(EntityUid gridId, uint uid)
        {
            if (!RemoveDecalHook(gridId, uid)) return false;

            if (!ChunkIndex.TryGetValue(gridId, out var values) || !values.TryGetValue(uid, out var indices))
            {
                return false;
            }

            var chunkCollection = ChunkCollection(gridId);
            if (chunkCollection == null || !chunkCollection.TryGetValue(indices, out var chunk) || !chunk.Decals.Remove(uid))
            {
                return false;
            }

            if (chunk.Decals.Count == 0)
                chunkCollection.Remove(indices);

            ChunkIndex[gridId].Remove(uid);
            DirtyChunk(gridId, indices, chunk);
            return true;
        }

        protected virtual bool RemoveDecalHook(EntityUid gridId, uint uid) => true;
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
