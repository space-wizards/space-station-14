using Robust.Server.AI;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(MoverComponent))]
    public class AiControllerComponent : MoverComponent
    {
        private string _logicName;
        private float _visionRadius;

        public override string Name => "AiController";

        [ViewVariables(VVAccess.ReadWrite)]
        public string LogicName
        {
            get => _logicName;
            set
            {
                _logicName = value;
                Processor = null;
            }
        }

        public AiLogicProcessor Processor { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float VisionRadius
        {
            get => _visionRadius;
            set => _visionRadius = value;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            // This component requires a physics component.
            if (!Owner.HasComponent<PhysicsComponent>())
                Owner.AddComponent<PhysicsComponent>();
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _logicName, "logic", null);
            serializer.DataField(ref _visionRadius, "vision", 8.0f);
        }


        public override void SetVelocityDirection(Direction direction, bool enabled) { }
    }
}
