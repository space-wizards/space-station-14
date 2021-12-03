using Content.Server.Atmos.Components;
using Content.Server.Kudzu;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public class AirtightSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly SpreaderSystem _spreaderSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AirtightComponent, ComponentInit>(OnAirtightInit);
            SubscribeLocalEvent<AirtightComponent, ComponentShutdown>(OnAirtightShutdown);
            SubscribeLocalEvent<AirtightComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<AirtightComponent, AnchorStateChangedEvent>(OnAirtightPositionChanged);
            SubscribeLocalEvent<AirtightComponent, RotateEvent>(OnAirtightRotated);
        }

        private void OnAirtightInit(EntityUid uid, AirtightComponent airtight, ComponentInit args)
        {
            if (airtight.FixAirBlockedDirectionInitialize)
            {
                var rotateEvent = new RotateEvent(airtight.Owner, Angle.Zero, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(airtight.Owner).WorldRotation);
                OnAirtightRotated(uid, airtight, ref rotateEvent);
            }

            // Adding this component will immediately anchor the entity, because the atmos system
            // requires airtight entities to be anchored for performance.
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(airtight.Owner).Anchored = true;

            UpdatePosition(airtight);
        }

        private void OnAirtightShutdown(EntityUid uid, AirtightComponent airtight, ComponentShutdown args)
        {
            SetAirblocked(airtight, false);

            InvalidatePosition(airtight.LastPosition.Item1, airtight.LastPosition.Item2, airtight.FixVacuum);
            RaiseLocalEvent(new AirtightChanged(airtight));
        }

        private void OnMapInit(EntityUid uid, AirtightComponent airtight, MapInitEvent args)
        {
        }

        private void OnAirtightPositionChanged(EntityUid uid, AirtightComponent airtight, ref AnchorStateChangedEvent args)
        {
            var gridId = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(airtight.Owner).GridID;
            var coords = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(airtight.Owner).Coordinates;

            var grid = _mapManager.GetGrid(gridId);
            var tilePos = grid.TileIndicesFor(coords);

            // Update and invalidate new position.
            airtight.LastPosition = (gridId, tilePos);
            InvalidatePosition(gridId, tilePos);
        }

        private void OnAirtightRotated(EntityUid uid, AirtightComponent airtight, ref RotateEvent ev)
        {
            if (!airtight.RotateAirBlocked || airtight.InitialAirBlockedDirection == (int)AtmosDirection.Invalid)
                return;

            airtight.CurrentAirBlockedDirection = (int) Rotate((AtmosDirection)airtight.InitialAirBlockedDirection, ev.NewRotation);
            UpdatePosition(airtight);
            RaiseLocalEvent(uid, new AirtightChanged(airtight));
        }

        public void SetAirblocked(AirtightComponent airtight, bool airblocked)
        {
            airtight.AirBlocked = airblocked;
            UpdatePosition(airtight);
            RaiseLocalEvent(airtight.OwnerUid, new AirtightChanged(airtight));
        }

        public void UpdatePosition(AirtightComponent airtight)
        {
            if (!IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(airtight.Owner).Anchored || !IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(airtight.Owner).GridID.IsValid())
                return;

            var grid = _mapManager.GetGrid(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(airtight.Owner).GridID);
            airtight.LastPosition = (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(airtight.Owner).GridID, grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(airtight.Owner).Coordinates));
            InvalidatePosition(airtight.LastPosition.Item1, airtight.LastPosition.Item2, airtight.FixVacuum && !airtight.AirBlocked);
        }

        public void InvalidatePosition(GridId gridId, Vector2i pos, bool fixVacuum = false)
        {
            if (!gridId.IsValid())
                return;

            _atmosphereSystem.UpdateAdjacent(gridId, pos);
            _atmosphereSystem.InvalidateTile(gridId, pos);

            if(fixVacuum)
                _atmosphereSystem.FixVacuum(gridId, pos);
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

    public class AirtightChanged : EntityEventArgs
    {
        public AirtightComponent Airtight;

        public AirtightChanged(AirtightComponent airtight)
        {
            Airtight = airtight;
        }
    }
}
