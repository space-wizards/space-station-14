using Content.Server.Interfaces.GameObjects.Components.Movement;
using Robust.Server.AI;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Movement
{
    public class AiControllerComponent : Component, IMoverComponent
    {
        private string _logicName;
        private float _visionRadius;

        public override string Name => "AiController";

        public string LogicName => _logicName;
        public AiLogicProcessor Processor { get; set; }

        public float VisionRadius
        {
            get => _visionRadius;
            set => _visionRadius = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _logicName, "logic", null);
            serializer.DataField(ref _visionRadius, "vision", 8.0f);
        }
    }
}
