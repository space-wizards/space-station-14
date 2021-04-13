#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class AirtightComponent : Component, IMapInit
    {
        private (GridId, Vector2i) _lastPosition;
        private AtmosphereSystem _atmosphereSystem = default!;

        public override string Name => "Airtight";

        [DataField("airBlockedDirection", customTypeSerializer: typeof(FlagSerializer<AtmosDirectionFlags>))]
        [ViewVariables]
        private int _initialAirBlockedDirection = (int) AtmosDirection.All;

        [ViewVariables]
        private int _currentAirBlockedDirection;

        [DataField("airBlocked")]
        private bool _airBlocked = true;

        [DataField("fixVacuum")]
        private bool _fixVacuum = true;

        [ViewVariables]
        [DataField("rotateAirBlocked")]
        private bool _rotateAirBlocked = true;

        [ViewVariables]
        [DataField("fixAirBlockedDirectionInitialize")]
        private bool _fixAirBlockedDirectionInitialize = true;

        [ViewVariables]
        [field: DataField("noAirWhenFullyAirBlocked")]
        public bool NoAirWhenFullyAirBlocked { get; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool AirBlocked
        {
            get => _airBlocked;
            set
            {
                _airBlocked = value;

                UpdatePosition();
            }
        }

        public AtmosDirection AirBlockedDirection
        {
            get => (AtmosDirection)_currentAirBlockedDirection;
            set
            {
                _currentAirBlockedDirection = (int) value;
                _initialAirBlockedDirection = (int)Rotate(AirBlockedDirection, -Owner.Transform.LocalRotation);

                UpdatePosition();
            }
        }

        [ViewVariables]
        public bool FixVacuum => _fixVacuum;

        public override void Initialize()
        {
            base.Initialize();

            _atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            // Using the SnapGrid is critical for performance, and thus if it is absent the component
            // will not be airtight. A warning is much easier to track down than the object magically
            // not being airtight, so log one if the SnapGrid component is missing.
            Owner.EnsureComponentWarn(out SnapGridComponent _);

            if (_fixAirBlockedDirectionInitialize)
                RotateEvent(new RotateEvent(Owner, Angle.Zero, Owner.Transform.WorldRotation));

            UpdatePosition();
        }

        public void RotateEvent(RotateEvent ev)
        {
            if (!_rotateAirBlocked || ev.Sender != Owner || _initialAirBlockedDirection == (int)AtmosDirection.Invalid)
                return;

            _currentAirBlockedDirection = (int) Rotate((AtmosDirection)_initialAirBlockedDirection, ev.NewRotation);
            UpdatePosition();
        }

        private AtmosDirection Rotate(AtmosDirection myDirection, Angle myAngle)
        {
            var newAirBlockedDirs = AtmosDirection.Invalid;

            if (myAngle == Angle.Zero)
                return myDirection;

            // TODO ATMOS MULTIZ When we make multiZ atmos, special case this.
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

        public void MapInit()
        {
            UpdatePosition();
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            _airBlocked = false;

            UpdatePosition(_lastPosition.Item1, _lastPosition.Item2);

            if (_fixVacuum)
            {
                _atmosphereSystem.GetGridAtmosphere(_lastPosition.Item1)?.FixVacuum(_lastPosition.Item2);
            }
        }

        public void OnSnapGridMove(SnapGridPositionChangedEvent ev)
        {
            // Invalidate old position.
            UpdatePosition(ev.OldGrid, ev.OldPosition);

            // Update and invalidate new position.
            _lastPosition = (ev.NewGrid, ev.Position);
            UpdatePosition(ev.NewGrid, ev.Position);
        }

        private void UpdatePosition()
        {
            if (Owner.TryGetComponent(out SnapGridComponent? snapGrid))
            {
                _lastPosition = (Owner.Transform.GridID, snapGrid.Position);
                UpdatePosition(Owner.Transform.GridID, snapGrid.Position);
            }
        }

        private void UpdatePosition(GridId gridId, Vector2i pos)
        {
            var gridAtmos = _atmosphereSystem.GetGridAtmosphere(gridId);

            gridAtmos?.UpdateAdjacentBits(pos);
            gridAtmos?.Invalidate(pos);
        }
    }
}
