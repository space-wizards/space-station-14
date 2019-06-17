using System;
using Content.Server.Interfaces.Atmos;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class AirtightComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IAtmosphereMap _atmosphereMap;
#pragma warning restore 649

        public override string Name => "Airtight";

        public bool UseAdjacentAtmosphere;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref UseAdjacentAtmosphere, "adjacentAtmosphere", false);
        }

        public override void Initialize()
        {
            base.Initialize();

            // Using the SnapGrid is critical for the performance of the room builder, and thus if
            // it is absent the component will not be airtight. An exception is much easier to track
            // down than the object magically not being airtight, so throw one if the SnapGrid component
            // is missing.
            if (!Owner.HasComponent<SnapGridComponent>())
                throw new Exception("Airtight entities must have a SnapGrid component");
        }

        protected override void Startup()
        {
            base.Startup();
            Owner.Transform.OnMove += OnTransformMove;
            UpdatePosition();
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            Owner.Transform.OnMove -= OnTransformMove;
            UpdatePosition();
        }

        private void OnTransformMove(object sender, MoveEventArgs args)
        {
            UpdatePosition(args.OldPosition);
            UpdatePosition(args.NewPosition);
        }

        private void UpdatePosition() => UpdatePosition(Owner.Transform.GridPosition);

        private void UpdatePosition(GridCoordinates pos)
        {
            _atmosphereMap.GetGridAtmosphereManager(Owner.Transform.GridID).Invalidate(pos);
        }
    }
}
