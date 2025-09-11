using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.GameStates;
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

        protected bool PvsEnabled;

        // Note that this constant is effectively baked into all map files, because of how they save the grid decal component.
        // So if this ever needs changing, the maps need converting.
        public const int ChunkSize = 32;
        public static Vector2i GetChunkIndices(Vector2 coordinates) => new ((int) Math.Floor(coordinates.X / ChunkSize), (int) Math.Floor(coordinates.Y / ChunkSize));

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
            SubscribeLocalEvent<DecalGridComponent, ComponentStartup>(OnCompStartup);
            SubscribeLocalEvent<DecalGridComponent, ComponentGetState>(OnGetState);
        }

        private void OnGetState(EntityUid uid, DecalGridComponent component, ref ComponentGetState args)
        {
            if (PvsEnabled && !args.ReplayState)
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

            args.State = new DecalGridDeltaState(data, new(component.ChunkCollection.ChunkCollection.Keys));
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

            // This **shouldn't** be required, but just in case we ever get entity prototypes that have decal grids, we
            // need to ensure that we send an initial full state to players.
            Dirty(uid, component);
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

        public virtual HashSet<(uint Index, Decal Decal)> GetDecalsInRange(EntityUid gridId, Vector2 position, float distance = 0.75f, Func<Decal, bool>? validDelegate = null)
        {
            // NOOP on client atm.
            return new HashSet<(uint Index, Decal Decal)>();
        }

        public virtual bool RemoveDecal(EntityUid gridId, uint decalId, DecalGridComponent? component = null)
        {
            // NOOP on client atm.
            return true;
        }
    }

    /// <summary>
    ///     Sent by clients to request that a decal is placed on the server.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestDecalPlacementEvent : EntityEventArgs
    {
        public Decal Decal;
        public NetCoordinates Coordinates;

        public RequestDecalPlacementEvent(Decal decal, NetCoordinates coordinates)
        {
            Decal = decal;
            Coordinates = coordinates;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RequestDecalRemovalEvent : EntityEventArgs
    {
        public NetCoordinates Coordinates;

        public RequestDecalRemovalEvent(NetCoordinates coordinates)
        {
            Coordinates = coordinates;
        }
    }
}
