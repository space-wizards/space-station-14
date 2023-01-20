using System.Diagnostics.CodeAnalysis;
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

        // Note that this constant is effectively baked into all map files, because of how they save the grid decal component.
        // So if this ever needs changing, the maps need converting.
        public const int ChunkSize = 32;
        public static Vector2i GetChunkIndices(Vector2 coordinates) => new ((int) Math.Floor(coordinates.X / ChunkSize), (int) Math.Floor(coordinates.Y / ChunkSize));

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
            SubscribeLocalEvent<DecalGridComponent, ComponentStartup>(OnCompStartup);
        }

        private void OnGridInitialize(GridInitializeEvent msg)
        {
            EnsureComp<DecalGridComponent>(msg.EntityUid);
        }
        
        private void OnCompStartup(EntityUid uid, DecalGridComponent component, ComponentStartup args)
        {
            foreach (var (indices, decals) in component.ChunkCollection.ChunkCollection)
            {
                foreach (var decalUid in decals.Decals.Keys)
                {
                    component.DecalIndex[decalUid] = indices;
                }
            }
        }

        protected Dictionary<Vector2i, DecalChunk>? ChunkCollection(EntityUid gridEuid, DecalGridComponent? comp = null)
        {
            if (!Resolve(gridEuid, ref comp))
                return null;

            return comp.ChunkCollection.ChunkCollection;
        }

        protected virtual void DirtyChunk(EntityUid id, Vector2i chunkIndices, DecalChunk chunk) {}

        // internal, so that client/predicted code doesn't accidentally remove decals. There is a public server-side function.
        protected bool RemoveDecalInternal(EntityUid gridId, uint decalId, [NotNullWhen(true)] out Decal? removed, DecalGridComponent? component = null)
        {
            removed = null;
            if (!Resolve(gridId, ref component))
                return false;

            if (!component.DecalIndex.Remove(decalId, out var indices)
                || !component.ChunkCollection.ChunkCollection.TryGetValue(indices, out var chunk)
                || !chunk.Decals.Remove(decalId, out removed))
            {
                return false;
            }

            if (chunk.Decals.Count == 0)
                component.ChunkCollection.ChunkCollection.Remove(indices);

            DirtyChunk(gridId, indices, chunk);
            OnDecalRemoved(gridId, decalId, component, indices, chunk);
            return true;
        }

        protected virtual void OnDecalRemoved(EntityUid gridId, uint decalId, DecalGridComponent component, Vector2i indices, DecalChunk chunk)
        {
            // used by client-side overlay code
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
