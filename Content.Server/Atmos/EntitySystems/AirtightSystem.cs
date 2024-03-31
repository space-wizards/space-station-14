using Content.Server.Atmos.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class AirtightSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AirtightComponent, ComponentInit>(OnAirtightInit);
            SubscribeLocalEvent<AirtightComponent, ComponentShutdown>(OnAirtightShutdown);
            SubscribeLocalEvent<AirtightComponent, AnchorStateChangedEvent>(OnAirtightPositionChanged);
            SubscribeLocalEvent<AirtightComponent, ReAnchorEvent>(OnAirtightReAnchor);
            SubscribeLocalEvent<AirtightComponent, MoveEvent>(OnAirtightMoved);
        }

        private void OnAirtightInit(Entity<AirtightComponent> airtight, ref ComponentInit args)
        {
            var xform = EntityManager.GetComponent<TransformComponent>(airtight);

            if (airtight.Comp.FixAirBlockedDirectionInitialize)
            {
                var moveEvent = new MoveEvent(airtight, default, default, Angle.Zero, xform.LocalRotation, xform, false);
                if (AirtightMove(airtight, ref moveEvent))
                    return;
            }

            UpdatePosition(airtight);
        }

        private void OnAirtightShutdown(Entity<AirtightComponent> airtight, ref ComponentShutdown args)
        {
            var xform = Transform(airtight);

            // If the grid is deleting no point updating atmos.
            if (HasComp<MapGridComponent>(xform.GridUid) &&
                MetaData(xform.GridUid.Value).EntityLifeStage > EntityLifeStage.MapInitialized)
            {
                return;
            }

            SetAirblocked(airtight, false, xform);
        }

        private void OnAirtightPositionChanged(EntityUid uid, AirtightComponent airtight, ref AnchorStateChangedEvent args)
        {
            var xform = args.Transform;

            if (!TryComp(xform.GridUid, out MapGridComponent? grid))
                return;

            var gridId = xform.GridUid;
            var coords = xform.Coordinates;

            var tilePos = grid.TileIndicesFor(coords);

            // Update and invalidate new position.
            airtight.LastPosition = (gridId.Value, tilePos);
            InvalidatePosition(gridId.Value, tilePos);
        }

        private void OnAirtightReAnchor(EntityUid uid, AirtightComponent airtight, ref ReAnchorEvent args)
        {
            foreach (var gridId in new[] { args.OldGrid, args.Grid })
            {
                // Update and invalidate new position.
                airtight.LastPosition = (gridId, args.TilePos);
                InvalidatePosition(gridId, args.TilePos);
            }
        }

        private void OnAirtightMoved(Entity<AirtightComponent> airtight, ref MoveEvent ev)
        {
            AirtightMove(airtight, ref ev);
        }

        private bool AirtightMove(Entity<AirtightComponent> ent, ref MoveEvent ev)
        {
            var (owner, airtight) = ent;

            airtight.CurrentAirBlockedDirection = (int) Rotate((AtmosDirection)airtight.InitialAirBlockedDirection, ev.NewRotation);
            var pos = airtight.LastPosition;
            UpdatePosition(ent, ev.Component);
            var airtightEv = new AirtightChanged(owner, airtight, pos);
            RaiseLocalEvent(owner, ref airtightEv, true);
            return true;
        }

        public void SetAirblocked(Entity<AirtightComponent> airtight, bool airblocked, TransformComponent? xform = null)
        {
            if (airtight.Comp.AirBlocked == airblocked)
                return;

            if (!Resolve(airtight, ref xform))
                return;

            var pos = airtight.Comp.LastPosition;
            airtight.Comp.AirBlocked = airblocked;
            UpdatePosition(airtight, xform);
            var airtightEv = new AirtightChanged(airtight, airtight, pos);
            RaiseLocalEvent(airtight, ref airtightEv, true);
        }

        public void UpdatePosition(Entity<AirtightComponent> ent, TransformComponent? xform = null)
        {
            var (owner, airtight) = ent;
            if (!Resolve(owner, ref xform))
                return;

            if (!xform.Anchored || !TryComp(xform.GridUid, out MapGridComponent? grid))
                return;

            var indices = _transform.GetGridTilePositionOrDefault((ent, xform), grid);
            airtight.LastPosition = (xform.GridUid.Value, indices);
            InvalidatePosition((xform.GridUid.Value, grid), indices);
        }

        public void InvalidatePosition(Entity<MapGridComponent?> grid, Vector2i pos)
        {
            var query = EntityManager.GetEntityQuery<AirtightComponent>();
            _explosionSystem.UpdateAirtightMap(grid, pos, grid, query);
            _atmosphereSystem.InvalidateTile(grid.Owner, pos);
        }

        private AtmosDirection Rotate(AtmosDirection myDirection, Angle myAngle)
        {
            var newAirBlockedDirs = AtmosDirection.Invalid;

            if (myAngle == Angle.Zero)
                return myDirection;

            // TODO ATMOS MULTIZ: When we make multiZ atmos, special case this.
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!myDirection.IsFlagSet(direction))
                    continue;
                var angle = direction.ToAngle();
                angle += myAngle;
                newAirBlockedDirs |= angle.ToAtmosDirectionCardinal();
            }

            return newAirBlockedDirs;
        }
    }

    [ByRefEvent]
    public readonly record struct AirtightChanged(EntityUid Entity, AirtightComponent Airtight,
        (EntityUid Grid, Vector2i Tile) Position);
}
