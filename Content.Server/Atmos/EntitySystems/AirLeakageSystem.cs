using Content.Server.Atmos.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed class AirLeakageSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AirLeakageComponent, ComponentInit>(OnAirLeakageInit);
            SubscribeLocalEvent<AirLeakageComponent, ComponentShutdown>(OnAirLeakageShutdown);
            SubscribeLocalEvent<AirLeakageComponent, AnchorStateChangedEvent>(OnAirLeakagePositionChanged);
            SubscribeLocalEvent<AirLeakageComponent, ReAnchorEvent>(OnAirLeakageReAnchor);
            SubscribeLocalEvent<AirLeakageComponent, MoveEvent>(OnAirLeakageMoved);
        }

        private void OnAirLeakageInit(Entity<AirLeakageComponent> ent, ref ComponentInit args)
        {
            var xform = EntityManager.GetComponent<TransformComponent>(ent);
            UpdatePosition(ent, xform);
        }

        private void OnAirLeakageShutdown(Entity<AirLeakageComponent> ent, ref ComponentShutdown args)
        {
            var xform = Transform(ent);

            // If the grid is deleting no point updating atmos.
            if (HasComp<MapGridComponent>(xform.GridUid) &&
                MetaData(xform.GridUid.Value).EntityLifeStage > EntityLifeStage.MapInitialized)
            {
                return;
            }

            UpdatePosition(ent, xform);
        }

        private void OnAirLeakagePositionChanged(Entity<AirLeakageComponent> ent, ref AnchorStateChangedEvent args)
        {
            var xform = args.Transform;

            if (!TryComp(xform.GridUid, out MapGridComponent? grid))
                return;

            var gridId = xform.GridUid;
            var coords = xform.Coordinates;

            var tilePos = grid.TileIndicesFor(coords);

            _atmosphereSystem.InvalidateTile(gridId.Value, tilePos);
        }

        private void OnAirLeakageReAnchor(Entity<AirLeakageComponent> ent, ref ReAnchorEvent args)
        {
            foreach (var gridId in new[] { args.OldGrid, args.Grid })
            {
                _atmosphereSystem.InvalidateTile(gridId, args.TilePos);
            }
        }
        private void OnAirLeakageMoved(Entity<AirLeakageComponent> ent, ref MoveEvent ev)
        {
            UpdatePosition(ent, ev.Component);
        }

        public void UpdatePosition(Entity<AirLeakageComponent> ent, TransformComponent? xform = null)
        {
            if (!Resolve(ent.Owner, ref xform))
                return;

            if (!xform.Anchored || !TryComp(xform.GridUid, out MapGridComponent? grid))
                return;

            var indices = _transform.GetGridTilePositionOrDefault((ent, xform), grid);
            ent.Comp.LastPosition = (xform.GridUid.Value, indices);

            _atmosphereSystem.InvalidateTile(xform.GridUid.Value, indices);
        }
    }
}
