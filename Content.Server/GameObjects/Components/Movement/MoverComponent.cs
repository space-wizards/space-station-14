using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Movement
{
    public abstract class MoverComponent: Component
    {
        const float DefaultBaseWalkSpeed = 4.0f;
        const float DefaultBaseSprintSpeed = 7.0f;
        private float _baseWalkSpeed = DefaultBaseWalkSpeed;
        /// <summary>
        ///     Movement speed (m/s) that the entity walks, before modifiers
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseWalkSpeed
        {
            get
            {
                return _baseWalkSpeed;
            }
            set
            {
                MarkMovementSpeedModifiersDirty();
                _baseWalkSpeed = value;
            }
        }
        private float _baseSprintSpeed = DefaultBaseSprintSpeed;
        /// <summary>
        ///     Movement speed (m/s) that the entity sprints, before modifiers
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseSprintSpeed
        {
            get
            {
                return _baseSprintSpeed;
            }
            set
            {
                MarkMovementSpeedModifiersDirty();
                _baseSprintSpeed = value;
            }
        }

        private float _currentWalkSpeed;
        /// <summary>
        ///     Movement speed (m/s) that the entity walks, after modifiers
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public float CurrentWalkSpeed
        {
            get
            {
                RecalculateMovementSpeed();
                return _currentWalkSpeed;
            }
        }

        private float _currentSprintSpeed;
        /// <summary>
        ///     Movement speed (m/s) that the entity sprints.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public float CurrentSprintSpeed
        {
            get
            {
                RecalculateMovementSpeed();
                return _currentSprintSpeed;
            }
        }
        /// <summary>
        ///     Do we need to recalculate walk/sprint speed due to modifier changes?
        /// </summary>
        private bool _movementModifiersDirty = true;

        /// <summary>
        ///     tells the component to recalculate movement speed when next used
        /// </summary>
        public void MarkMovementSpeedModifiersDirty()
        {
            _movementModifiersDirty = true;
        }

        /// <summary>
        ///     Recalculate movement speed with current modifiers, or return early if no change
        /// </summary>
        private void RecalculateMovementSpeed()
        {
            if (!_movementModifiersDirty)
                return;
            var movespeedModifiers = Owner.GetAllComponents<IMoveSpeedModifier>();
            float walkSpeed = BaseWalkSpeed;
            float sprintSpeed = BaseWalkSpeed;
            foreach (var component in movespeedModifiers)
            {
                walkSpeed *= component.WalkSpeedModifier;
                sprintSpeed *= component.SprintSpeedModifier;
            }
            _currentWalkSpeed = walkSpeed;
            _currentSprintSpeed = sprintSpeed;
            _movementModifiersDirty = false;
        }
        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            //only save the base speeds - the current speeds are transient.
            serializer.DataReadWriteFunction("wspd", DefaultBaseWalkSpeed, value => BaseWalkSpeed = value, () => BaseWalkSpeed);
            serializer.DataReadWriteFunction("rspd", DefaultBaseSprintSpeed, value => BaseSprintSpeed = value, () => BaseSprintSpeed);

            // The velocity and moving directions is usually set from player or AI input,
            // so we don't want to save/load these derived fields.
        }
        /// <summary>
        ///     Is the entity Sprinting (running)?
        /// </summary>
        [ViewVariables]
        public bool Sprinting { get; set; } = true;

        /// <summary>
        ///     Calculated linear velocity direction of the entity.
        /// </summary>
        [ViewVariables]
        public Vector2 VelocityDir { get; set; }
        public abstract void SetVelocityDirection(Direction direction, bool enabled);
        public GridCoordinates LastPosition { get; set; }
        public float StepSoundDistance { get; set; }
    }
    interface IMoveSpeedModifier
    {
        float WalkSpeedModifier { get; }
        float SprintSpeedModifier { get; }
    }

}
