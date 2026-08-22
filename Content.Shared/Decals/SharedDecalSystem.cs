using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Decals
{
    public abstract partial class SharedDecalSystem : EntitySystem
    {
        [Dependency] protected ChunkEntitySystem ChunkEntities = default!;
        [Dependency] protected EntityQuery<DecalChunkComponent> DecalChunkQuery = default!;

        protected bool PvsEnabled;

        // Legacy DecalGridComponent data was serialized in 32x32 chunks. Loading code must treat those keys as old
        // storage buckets and re-chunk decals by coordinates before migrating them to chunk entities.
        public const int LegacyChunkSize = 32;

        private List<(DecalIndex Index, Decal Decal)> _tempDecals = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DecalGridComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<DecalChunkComponent, ComponentStartup>(OnChunkStartup);
            SubscribeAllEvent<RequestDecalPlacementEvent>(OnDecalPlacementRequest);
            SubscribeAllEvent<RequestDecalRemovalEvent>(OnDecalRemovalRequest);
        }

        protected abstract void OnDecalPlacementRequest(RequestDecalPlacementEvent ev, EntitySessionEventArgs eventArgs);

        protected abstract void OnDecalRemovalRequest(RequestDecalRemovalEvent ev, EntitySessionEventArgs eventArgs);

        private void OnChunkStartup(Entity<DecalChunkComponent> ent, ref ComponentStartup args)
        {
            RebuildFreeDecalIds(ent.Comp);
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

            var data = new Dictionary<Vector2i, DecalGridComponent.DecalChunk>();
            foreach (var (index, chunk) in component.ChunkCollection.ChunkCollection)
            {
                if (chunk.LastModified >= args.FromTick)
                    data[index] = chunk;
            }

            args.State = new DecalGridDeltaState(data, new(component.ChunkCollection.ChunkCollection.Keys));
        }

        public HashSet<(DecalIndex Index, Decal Decal)> GetDecalsInRange(EntityUid gridId, Vector2 position, float distance = 0.75f, Func<Decal, bool>? validDelegate = null)
        {
            var bounds = new Box2(position - new Vector2(distance + 1f), position + new Vector2(distance + 1f));
            var decalIds = GetDecalsIntersecting(gridId, bounds);

            decalIds.RemoveWhere(set =>
                (position - set.Decal.Coordinates - new Vector2(0.5f, 0.5f)).Length() > distance ||
                validDelegate != null && !validDelegate(set.Decal));

            return decalIds;
        }

        public HashSet<(DecalIndex Index, Decal Decal)> GetDecalsIntersecting(EntityUid gridUid, Box2 bounds)
        {
            var decalIds = new HashSet<(DecalIndex, Decal)>();

            foreach (var chunk in ChunkEntities.GetChunksIntersecting(gridUid, bounds, DecalChunkQuery))
            {
                foreach (var (id, decal) in chunk.Comp2.Decals)
                {
                    if (!bounds.Contains(decal.Coordinates))
                        continue;

                    decalIds.Add((new DecalIndex(chunk.Comp1.Chunk, id), decal));
                }
            }

            return decalIds;
        }

        public void GetDecalsAt(EntityUid gridUid, string id, Vector2 coordinates, Angle angle, ICollection<(DecalIndex Index, Decal Decal)> matches)
        {
            var chunkIndices = ChunkEntitySystem.GetChunkIndices(coordinates);

            if (!ChunkEntities.TryGetChunk(gridUid, chunkIndices, out var chunk) ||
                !DecalChunkQuery.TryComp(chunk.Value.Owner, out var decals))
            {
                return;
            }

            foreach (var (chunkDecalId, decal) in decals.Decals)
            {
                if (decal.Id != id ||
                    decal.Coordinates != coordinates ||
                    decal.Angle != angle)
                {
                    continue;
                }

                matches.Add((new DecalIndex(chunk.Value.Comp.Chunk, chunkDecalId), decal));
            }
        }

        public bool TryGetDecalAt(
            EntityUid gridUid,
            string id,
            Vector2 coordinates,
            Angle angle,
            out DecalIndex decalIndex,
            [NotNullWhen(true)] out Decal? foundDecal)
        {
            decalIndex = default;
            foundDecal = null;
            GetDecalsAt(gridUid, id, coordinates, angle, _tempDecals);

            foreach (var (index, decal) in _tempDecals)
            {
                decalIndex = index;
                foundDecal = decal;
                break;
            }

            _tempDecals.Clear();

            return foundDecal != null;
        }

        public virtual bool RemoveDecal(EntityUid gridId, DecalIndex decal)
        {
            // NOOP on client atm.
            return true;
        }

        /// <summary>
        /// Adds a decal.
        /// </summary>
        public abstract bool TryAddDecal(Decal decal, EntityCoordinates coordinates, out DecalIndex decalId);

        private static void RebuildFreeDecalIds(DecalChunkComponent component)
        {
            component.FreeDecalIds.Clear();

            // Saves only carry decal data and the highest allocated server id. The free list is a runtime cache.
            foreach (var id in component.Decals.Keys)
            {
                if (id <= DecalChunkComponent.MaxServerDecalId)
                    component.MaxDecalId = Math.Max(component.MaxDecalId, id);
            }

            component.FreeDecalIds.EnsureCapacity(component.MaxDecalId + 1 - component.Decals.Count);

            for (var id = 0; id <= component.MaxDecalId; id++)
            {
                var decalId = (ushort) id;
                if (!component.Decals.ContainsKey(decalId))
                    component.FreeDecalIds.Add(decalId);
            }

            // Allocation pops from the end, so keep the lowest free id there for stable decal ids.
            component.FreeDecalIds.Sort((x, y) => y.CompareTo(x));
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
