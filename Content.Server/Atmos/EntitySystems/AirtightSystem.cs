using Content.Server.Atmos.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

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
            SubscribeLocalEvent<AirtightComponent, MoveEvent>(OnAirtightRotated);
        }

        private void OnAirtightInit(EntityUid uid, AirtightComponent airtight, ComponentInit args)
        {
            var xform = EntityManager.GetComponent<TransformComponent>(uid);

            if (airtight.FixAirBlockedDirectionInitialize)
            {
                var moveEvent = new MoveEvent(airtight.Owner, default, default, Angle.Zero, xform.LocalRotation, xform, false);
                OnAirtightRotated(uid, airtight, ref moveEvent);
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

            SetAirblocked(airtight, false, xform);
        }

        private void OnAirtightPositionChanged(EntityUid uid, AirtightComponent airtight, ref AnchorStateChangedEvent args)
        {
            var xform = Transform(uid);

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

        private void OnAirtightRotated(EntityUid uid, AirtightComponent airtight, ref MoveEvent ev)
        {
            if (!airtight.RotateAirBlocked || airtight.InitialAirBlockedDirection == (int)AtmosDirection.Invalid)
                return;

            airtight.CurrentAirBlockedDirection = (int) Rotate((AtmosDirection)airtight.InitialAirBlockedDirection, ev.NewRotation);
            UpdatePosition(airtight, ev.Component);
            RaiseLocalEvent(uid, new AirtightChanged(airtight), true);
        }

        public void SetAirblocked(AirtightComponent airtight, bool airblocked, TransformComponent? xform = null)
        {
            if (airtight.AirBlocked == airblocked)
                return;

            if (!Resolve(airtight.Owner, ref xform)) return;

            airtight.AirBlocked = airblocked;
            UpdatePosition(airtight, xform);
            RaiseLocalEvent(airtight.Owner, new AirtightChanged(airtight), true);
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

    public sealed class AirtightChanged : EntityEventArgs
    {
        public AirtightComponent Airtight;

        public AirtightChanged(AirtightComponent airtight)
        {
            Airtight = airtight;
        }
    }
}
