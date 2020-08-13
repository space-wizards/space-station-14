using System;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class AirtightComponent : Component, IMapInit
    {
        private SnapGridComponent _snapGrid;
        private (GridId, MapIndices) _lastPosition;

        public override string Name => "Airtight";

        private bool _airBlocked = true;
        private bool _fixVacuum = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool AirBlocked
        {
            get => _airBlocked;
            set
            {
                _airBlocked = value;
                EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.GridID)?.Invalidate(_snapGrid.Position);
            }
        }

        [ViewVariables]
        public bool FixVacuum => _fixVacuum;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _airBlocked, "airBlocked", true);
            serializer.DataField(ref _fixVacuum, "fixVacuum", false);
        }

        public override void Initialize()
        {
            base.Initialize();

            // Using the SnapGrid is critical for the performance of the room builder, and thus if
            // it is absent the component will not be airtight. An exception is much easier to track
            // down than the object magically not being airtight, so throw one if the SnapGrid component
            // is missing.
            if (!Owner.TryGetComponent(out _snapGrid))
                throw new Exception("Airtight entities must have a SnapGrid component");

            UpdatePosition();
        }

        public override void OnRemove()
        {
            base.OnRemove();

            _airBlocked = false;

            UpdatePosition();
        }

        public void MapInit()
        {
            _snapGrid.OnPositionChanged += OnTransformMove;
            _lastPosition = (Owner.Transform.GridID, _snapGrid.Position);
            UpdatePosition();
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            _airBlocked = false;

            _snapGrid.OnPositionChanged -= OnTransformMove;
            UpdatePosition();
        }

        private void OnTransformMove()
        {
            UpdatePosition(_lastPosition.Item1, _lastPosition.Item2);
            UpdatePosition();
            _lastPosition = (Owner.Transform.GridID, _snapGrid.Position);
        }

        private void UpdatePosition() => UpdatePosition(Owner.Transform.GridID, _snapGrid.Position);

        private void UpdatePosition(GridId gridId, MapIndices pos)
        {
            EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(gridId)?.Invalidate(pos);
        }

    }
}
