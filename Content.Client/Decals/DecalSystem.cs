using System.Linq;
using System.Numerics;
using Content.Client.Decals.Overlays;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.Decals
{
    public sealed partial class DecalSystem : SharedDecalSystem
    {
        [Dependency] private IOverlayManager _overlayManager = default!;
        [Dependency] private SpriteSystem _sprites = default!;
        [Dependency] private SharedMapSystem _mapSystem = default!;
        [Dependency] private SharedTransformSystem _transform = default!;
        [Dependency] private TurfSystem _turf = default!;
        [Dependency] private EntityQuery<MapGridComponent> _gridQuery;

        private DecalOverlay? _overlay;

        /*
         * Client predicts entities from top of the chunk index down while server goes bottom-up.
         * This way we can minimise chances of overlap and be non-destructive to server states.
         */
        private ushort _nextPredictedDecal = ushort.MaxValue;
        private int _predictedDecalCount;

        private readonly List<ushort> _tempIds = new();

        public override void Initialize()
        {
            base.Initialize();

            _overlay = new DecalOverlay(_sprites, EntityManager, ProtoMan);
            _overlayManager.AddOverlay(_overlay);

            SubscribeLocalEvent<DecalChunkComponent, AfterAutoHandleStateEvent>(OnDecalChunkHandleState);
        }

        protected override void OnDecalPlacementRequest(RequestDecalPlacementEvent ev, EntitySessionEventArgs eventArgs)
        {
            var coordinates = GetCoordinates(ev.Coordinates);
            if (!coordinates.IsValid(EntityManager))
                return;

            AddPredictedDecal(ev.Decal, coordinates);
        }

        private bool AddPredictedDecal(Decal decal, EntityCoordinates coordinates)
        {
            if (!ProtoMan.HasIndex<DecalPrototype>(decal.Id))
                return false;

            var gridUid = _transform.GetGrid(coordinates);
            if (gridUid == null || !_gridQuery.TryComp(gridUid.Value, out var grid))
                return false;

            if (_turf.IsSpace(_mapSystem.GetTileRef(gridUid.Value, grid, coordinates)))
                return false;

            if (!TryAllocatePredictedDecalId(gridUid.Value, out var decalId))
                return false;

            var chunk = ChunkEntities.GetOrCreateChunk(gridUid.Value, ChunkEntitySystem.GetChunkIndices(decal.Coordinates));
            var decals = EnsureComp<DecalChunkComponent>(chunk.Owner);
            decals.Decals[decalId] = decal;
            _predictedDecalCount++;
            return true;
        }

        protected override void OnDecalRemovalRequest(RequestDecalRemovalEvent ev, EntitySessionEventArgs eventArgs)
        {
            var coordinates = GetCoordinates(ev.Coordinates);
            if (!coordinates.IsValid(EntityManager))
                return;

            var gridUid = _transform.GetGrid(coordinates);
            if (gridUid == null)
                return;

            foreach (var (decalId, _) in GetDecalsInRange(gridUid.Value, ev.Coordinates.Position))
            {
                RemoveDecal(gridUid.Value, decalId);
            }
        }

        private void OnDecalChunkHandleState(Entity<DecalChunkComponent> ent, ref AfterAutoHandleStateEvent args)
        {
            var removed = 0;
            _tempIds.Clear();
            _tempIds.AddRange(ent.Comp.Decals.Keys);

            foreach (var id in _tempIds)
            {
                if (id < DecalChunkComponent.MinPredictedDecalId)
                    continue;

                ent.Comp.Decals.Remove(id);
                removed++;
            }

            if (removed == 0)
                return;

            _predictedDecalCount = Math.Max(0, _predictedDecalCount - removed);
            if (_predictedDecalCount == 0)
                _nextPredictedDecal = ushort.MaxValue;
        }

        private bool TryAllocatePredictedDecalId(EntityUid gridUid, out ushort decalId)
        {
            if (_predictedDecalCount >= DecalChunkComponent.PredictedDecalCount)
            {
                decalId = default;
                return false;
            }

            for (var i = 0; i < DecalChunkComponent.PredictedDecalCount; i++)
            {
                var next = _nextPredictedDecal;
                _nextPredictedDecal = next == DecalChunkComponent.MinPredictedDecalId
                    ? ushort.MaxValue
                    : (ushort) (next - 1);

                if (PredictedDecalIdExists(gridUid, next))
                    continue;

                decalId = next;
                return true;
            }

            decalId = default;
            return false;
        }

        private bool PredictedDecalIdExists(EntityUid gridUid, ushort decalId)
        {
            foreach (var chunkEnt in ChunkEntities.GetChunks(gridUid))
            {
                if (!DecalChunkQuery.TryComp(chunkEnt.Owner, out var decals))
                    continue;

                if (decals.Decals.ContainsKey(decalId))
                    return true;
            }

            return false;
        }

        public override bool RemoveDecal(EntityUid gridId, DecalIndex decal)
        {
            if (!ChunkEntities.TryGetChunk(gridId, decal.Chunk, out var chunkEnt) ||
                !DecalChunkQuery.TryComp(chunkEnt.Value.Owner, out var decals) ||
                !decals.Decals.Remove(decal.Id))
            {
                return false;
            }

            if (decal.Id >= DecalChunkComponent.MinPredictedDecalId)
            {
                _predictedDecalCount = Math.Max(0, _predictedDecalCount - 1);
                if (_predictedDecalCount == 0)
                    _nextPredictedDecal = ushort.MaxValue;
            }

            return true;
        }

        public void ToggleOverlay()
        {
            if (_overlay == null)
                return;

            if (_overlayManager.HasOverlay<DecalOverlay>())
            {
                _overlayManager.RemoveOverlay(_overlay);
            }
            else
            {
                _overlayManager.AddOverlay(_overlay);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            if (_overlay == null)
                return;

            _overlayManager.RemoveOverlay(_overlay);
        }
    }
}
