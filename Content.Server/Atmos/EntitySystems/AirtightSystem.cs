using Content.Server.Atmos.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Shared.Destructible;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class AirtightSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
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

        private void OnAirtightInit(EntityUid uid, AirtightComponent airtight, ComponentInit args)
        {
            var xform = EntityManager.GetComponent<TransformComponent>(uid);

            if (airtight.FixAirBlockedDirectionInitialize)
            {
                var moveEvent = new MoveEvent(uid, default, default, Angle.Zero, xform.LocalRotation, xform, false);
                if (AirtightMove(uid, airtight, ref moveEvent))
                    return;
            }

            UpdatePosition(airtight);
        }

        private void OnAirtightShutdown(EntityUid uid, AirtightComponent airtight, ComponentShutdown args)
        {
            var xform = Transform(uid);

            // If the grid is deleting no point updating atmos.
            if (_mapManager.TryGetGrid(xform.GridUid, out var grid))
            {
                if (MetaData(grid.Owner).EntityLifeStage > EntityLifeStage.MapInitialized) return;
            }

            SetAirblocked(uid, airtight, false, xform);
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

        private void OnAirtightMoved(EntityUid uid, AirtightComponent airtight, ref MoveEvent ev)
        {
            AirtightMove(uid, airtight, ref ev);
        }

        private bool AirtightMove(EntityUid uid, AirtightComponent airtight, ref MoveEvent ev)
        {
            if (!airtight.RotateAirBlocked || airtight.InitialAirBlockedDirection == (int)AtmosDirection.Invalid)
                return false;

            airtight.CurrentAirBlockedDirection = (int) Rotate((AtmosDirection)airtight.InitialAirBlockedDirection, ev.NewRotation);
            var pos = airtight.LastPosition;
            UpdatePosition(airtight, ev.Component);
            var airtightEv = new AirtightChanged(uid, airtight, pos);
            RaiseLocalEvent(uid, ref airtightEv, true);
            return true;
        }

        public void SetAirblocked(EntityUid uid, AirtightComponent airtight, bool airblocked, TransformComponent? xform = null)
        {
            if (airtight.AirBlocked == airblocked)
                return;

            if (!Resolve(uid, ref xform))
                return;

            var pos = airtight.LastPosition;
            airtight.AirBlocked = airblocked;
            UpdatePosition(airtight, xform);
            var airtightEv = new AirtightChanged(uid, airtight, pos);
            RaiseLocalEvent(uid, ref airtightEv, true);
        }

        public void UpdatePosition(AirtightComponent airtight, TransformComponent? xform = null)
        {
            if (!Resolve(airtight.Owner, ref xform)) return;

            if (!xform.Anchored || !_mapManager.TryGetGrid(xform.GridUid, out var grid))
                return;

            airtight.LastPosition = (xform.GridUid.Value, grid.TileIndicesFor(xform.Coordinates));
            InvalidatePosition(airtight.LastPosition.Item1, airtight.LastPosition.Item2, airtight.FixVacuum && !airtight.AirBlocked);
        }

        public void InvalidatePosition(EntityUid gridId, Vector2i pos, bool fixVacuum = false)
        {
            if (!_mapManager.TryGetGrid(gridId, out var grid))
                return;

            var gridUid = grid.Owner;

            var query = EntityManager.GetEntityQuery<AirtightComponent>();
            _explosionSystem.UpdateAirtightMap(gridId, pos, grid, query);
            // TODO make atmos system use query
            _atmosphereSystem.InvalidateTile(gridUid, pos);
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
                if (!myDirection.IsFlagSet(direction)) continue;
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
