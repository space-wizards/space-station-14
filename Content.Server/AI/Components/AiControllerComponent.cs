using Content.Server.GameTicking;
using Content.Shared.Movement.Components;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.AI.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IMobMoverComponent))]
    public class AiControllerComponent : Component, IMobMoverComponent, IMoverComponent
    {
        [DataField("logic")] private float _visionRadius = 8.0f;

        public override string Name => "AiController";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("startingGear")]
        public string? StartingGearPrototype { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float VisionRadius
        {
            get => _visionRadius;
            set => _visionRadius = value;
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            // This component requires a physics component.
            Owner.EnsureComponent<PhysicsComponent>();
        }

        protected override void Startup()
        {
            base.Startup();

            if (StartingGearPrototype != null)
            {
                var gameTicker = EntitySystem.Get<GameTicker>();
                var protoManager = IoCManager.Resolve<IPrototypeManager>();

                var startingGear = protoManager.Index<StartingGearPrototype>(StartingGearPrototype);
                gameTicker.EquipStartingGear(Owner, startingGear, null);
            }
        }

        /// <summary>
        ///     Movement speed (m/s) that the entity walks, after modifiers
        /// </summary>
        [ViewVariables]
        public float CurrentWalkSpeed
        {
            get
            {
                if (Owner.TryGetComponent(out MovementSpeedModifierComponent? component))
                {
                    return component.CurrentWalkSpeed;
                }

                return MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
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
                if (Owner.TryGetComponent(out MovementSpeedModifierComponent? component))
                {
                    return component.CurrentSprintSpeed;
                }

                return MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
            }
        }

        public Angle LastGridAngle { get => Angle.Zero; set {} }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public float PushStrength { get; set; } = IMobMoverComponent.PushStrengthDefault;

        [ViewVariables(VVAccess.ReadWrite)]
        public float WeightlessStrength { get; set; } = IMobMoverComponent.WeightlessStrengthDefault;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public float GrabRange { get; set; } = IMobMoverComponent.GrabRangeDefault;

        /// <summary>
        ///     Is the entity Sprinting (running)?
        /// </summary>
        [ViewVariables]
        public bool Sprinting { get; } = true;

        /// <summary>
        ///     Calculated linear velocity direction of the entity.
        /// </summary>
        [ViewVariables]
        public Vector2 VelocityDir { get; set; }

        (Vector2 walking, Vector2 sprinting) IMoverComponent.VelocityDir =>
            Sprinting ? (Vector2.Zero, VelocityDir) : (VelocityDir, Vector2.Zero);

        public EntityCoordinates LastPosition { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float StepSoundDistance { get; set; }

        public void SetVelocityDirection(Direction direction, ushort subTick, bool enabled) { }
        public void SetSprinting(ushort subTick, bool walking) { }

        public virtual void Update(float frameTime) {}
    }
}
