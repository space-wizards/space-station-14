#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Atmos;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class AirtightComponent : Component, IMapInit
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private (GridId, MapIndices) _lastPosition;

        public override string Name => "Airtight";

        [ViewVariables]
        private int _airBlockedDirection;
        private bool _airBlocked = true;
        private bool _fixVacuum = false;

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
            get => (AtmosDirection)_airBlockedDirection;
            set
            {
                _airBlockedDirection = (int) value;

                UpdatePosition();
            }
        }

        [ViewVariables]
        public bool FixVacuum => _fixVacuum;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _airBlocked, "airBlocked", true);
            serializer.DataField(ref _fixVacuum, "fixVacuum", true);
            serializer.DataField(ref _airBlockedDirection, "airBlockedDirection", (int)AtmosDirection.All, WithFormat.Flags<AtmosDirectionFlags>());
        }

        public override void Initialize()
        {
            base.Initialize();

            // Using the SnapGrid is critical for performance, and thus if it is absent the component
            // will not be airtight. A warning is much easier to track down than the object magically
            // not being airtight, so log one if the SnapGrid component is missing.
            if (!Owner.EnsureComponent(out SnapGridComponent _))
                Logger.Warning($"Entity {Owner} at {Owner.Transform.MapPosition.ToString()} didn't have a {nameof(SnapGridComponent)}");

            UpdatePosition();
        }

        public void MapInit()
        {
            if (Owner.TryGetComponent(out SnapGridComponent? snapGrid))
            {
                snapGrid.OnPositionChanged += OnTransformMove;
                _lastPosition = (Owner.Transform.GridID, snapGrid.Position);
            }

            UpdatePosition();
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            _airBlocked = false;

            if (Owner.TryGetComponent(out SnapGridComponent? snapGrid))
            {
                snapGrid.OnPositionChanged -= OnTransformMove;
            }

            UpdatePosition(_lastPosition.Item1, _lastPosition.Item2);

            if (_fixVacuum)
                EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(_lastPosition.Item1)?.FixVacuum(_lastPosition.Item2);
        }

        private void OnTransformMove()
        {
            UpdatePosition(_lastPosition.Item1, _lastPosition.Item2);
            UpdatePosition();

            if (Owner.TryGetComponent(out SnapGridComponent? snapGrid))
            {
                _lastPosition = (Owner.Transform.GridID, snapGrid.Position);
            }
        }

        private void UpdatePosition()
        {
            if (Owner.TryGetComponent(out SnapGridComponent? snapGrid))
                UpdatePosition(Owner.Transform.GridID, snapGrid.Position);
        }

        private void UpdatePosition(GridId gridId, MapIndices pos)
        {
            var gridAtmos = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(gridId);

            if (gridAtmos == null) return;

            gridAtmos.UpdateAdjacentBits(pos);
            gridAtmos.Invalidate(pos);
        }
    }
}
