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
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;

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
            // TODO AIRTIGHT what FixAirBlockedDirectionInitialize even for?
            if (!airtight.Comp.FixAirBlockedDirectionInitialize)
            {
                UpdatePosition(airtight);
                return;
            }

            var xform = Transform(airtight);
            airtight.Comp.CurrentAirBlockedDirection =
                (int) Rotate((AtmosDirection) airtight.Comp.InitialAirBlockedDirection, xform.LocalRotation);
            UpdatePosition(airtight, xform);
            var airtightEv = new AirtightChanged(airtight, airtight, false, default);
            RaiseLocalEvent(airtight, ref airtightEv, true);
        }

        private void OnAirtightShutdown(Entity<AirtightComponent> airtight, ref ComponentShutdown args)
        {
            var xform = Transform(airtight);

            // If the grid is deleting no point updating atmos.
            if (xform.GridUid != null && LifeStage(xform.GridUid.Value) <= EntityLifeStage.MapInitialized)
                SetAirblocked(airtight, false, xform);
        }

        private void OnAirtightPositionChanged(EntityUid uid, AirtightComponent airtight, ref AnchorStateChangedEvent args)
        {
            var xform = args.Transform;

            if (!TryComp(xform.GridUid, out MapGridComponent? grid))
                return;

            var gridId = xform.GridUid;
            var coords = xform.Coordinates;
            var tilePos = _mapSystem.TileIndicesFor(gridId.Value, grid, coords);

            // Update and invalidate new position.
            airtight.LastPosition = (gridId.Value, tilePos);
            InvalidatePosition(gridId.Value, tilePos);

            var airtightEv = new AirtightChanged(uid, airtight, false, (gridId.Value, tilePos));
            RaiseLocalEvent(uid, ref airtightEv, true);
        }

        private void OnAirtightReAnchor(EntityUid uid, AirtightComponent airtight, ref ReAnchorEvent args)
        {
            foreach (var gridId in new[] { args.OldGrid, args.Grid })
            {
                // Update and invalidate new position.
                airtight.LastPosition = (gridId, args.TilePos);
                InvalidatePosition(gridId, args.TilePos);

                var airtightEv = new AirtightChanged(uid, airtight, false, (gridId, args.TilePos));
                RaiseLocalEvent(uid, ref airtightEv, true);
            }
        }

        private void OnAirtightMoved(Entity<AirtightComponent> ent, ref MoveEvent ev)
        {
            var (owner, airtight) = ent;
            airtight.CurrentAirBlockedDirection = (int) Rotate((AtmosDirection)airtight.InitialAirBlockedDirection, ev.NewRotation);
            var pos = airtight.LastPosition;
            UpdatePosition(ent, ev.Component);
            var airtightEv = new AirtightChanged(owner, airtight, false, pos);
            RaiseLocalEvent(owner, ref airtightEv, true);
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
            var airtightEv = new AirtightChanged(airtight, airtight, true, pos);
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

    /// <summary>
    /// Raised upon the airtight status being changed via anchoring, movement, etc.
    /// </summary>
    /// <param name="Entity"></param>
    /// <param name="Airtight"></param>
    /// <param name="AirBlockedChanged">Whether the <see cref="AirtightComponent.AirBlocked"/> changed</param>
    /// <param name="Position"></param>
    [ByRefEvent]
    public readonly record struct AirtightChanged(EntityUid Entity, AirtightComponent Airtight, bool AirBlockedChanged, (EntityUid Grid, Vector2i Tile) Position);
}
