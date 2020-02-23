using Content.Server.Interfaces.GameObjects.Components.Movement;
using Robust.Server.AI;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent, ComponentReference(typeof(IMoverComponent))]
    public class AiControllerComponent : Component, IMoverComponent
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

        /// <summary>
        ///     Movement speed (m/s) that the entity walks, before modifiers
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseWalkSpeed { get; set; } = PlayerInputMoverComponent.DefaultBaseWalkSpeed;

        /// <summary>
        ///     Movement speed (m/s) that the entity sprints, before modifiers
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseSprintSpeed { get; set; } = PlayerInputMoverComponent.DefaultBaseSprintSpeed;

        /// <summary>
        ///     Movement speed (m/s) that the entity walks, after modifiers
        /// </summary>
        [ViewVariables]
        public float CurrentWalkSpeed
        {
            get
            {
                float speed = BaseWalkSpeed;
                if (Owner.TryGetComponent<MovementSpeedModifierComponent>(out MovementSpeedModifierComponent component))
                {
                    speed *= component.WalkSpeedModifier;
                }
                return speed;
            }
        }
        /// <summary>
        ///     Movement speed (m/s) that the entity walks, after modifiers
        /// </summary>
        [ViewVariables]
        public float CurrentSprintSpeed
        {
            get
            {
                float speed = BaseSprintSpeed;
                if (Owner.TryGetComponent<MovementSpeedModifierComponent>(out MovementSpeedModifierComponent component))
                {
                    speed *= component.SprintSpeedModifier;
                }
                return speed;
            }
        }

        /// <summary>
        ///     Is the entity Sprinting (running)?
        /// </summary>
        [ViewVariables]
        public bool Sprinting { get; set; }

        /// <summary>
        ///     Calculated linear velocity direction of the entity.
        /// </summary>
        [ViewVariables]
        public Vector2 VelocityDir { get; set; }

        public GridCoordinates LastPosition { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float StepSoundDistance { get; set; }

        public void SetVelocityDirection(Direction direction, bool enabled) { }
    }
}
