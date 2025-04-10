using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components
{
    /// <summary>
    /// Applies basic movement speed and movement modifiers for an entity.
    /// If this is not present on the entity then they will use defaults for movement.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(MovementSpeedModifierSystem))]
    public sealed partial class MovementSpeedModifierComponent : Component
    {
        // Weightless
        public const float DefaultMinimumFrictionSpeed = 0.005f;
        public const float DefaultWeightlessFriction = 1f;
        public const float DefaultWeightlessFrictionNoInput = 0.2f;
        public const float DefaultOffGridFriction = 0.05f;
        public const float DefaultWeightlessModifier = 0.7f;
        public const float DefaultWeightlessAcceleration = 1f;

        public const float DefaultAcceleration = 20f;
        public const float DefaultFriction = 20f;
        public const float DefaultFrictionNoInput = 20f;

        public const float DefaultBaseWalkSpeed = 2.5f;
        public const float DefaultBaseSprintSpeed = 4.5f;

        [AutoNetworkedField, ViewVariables]
        public float WalkSpeedModifier = 1.0f;

        [AutoNetworkedField, ViewVariables]
        public float SprintSpeedModifier = 1.0f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _baseWalkSpeedVV
        {
            get => BaseWalkSpeed;
            set
            {
                BaseWalkSpeed = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        private float _baseSprintSpeedVV
        {
            get => BaseSprintSpeed;
            set
            {
                BaseSprintSpeed = value;
                Dirty();
            }
        }

        #region Weightless

        /// <summary>
        /// The negative velocity applied for friction when weightless and providing inputs.
        /// </summary>
        [AutoNetworkedField, DataField]
        public float BaseWeightlessFriction = DefaultWeightlessFriction;

        /// <summary>
        /// The negative velocity applied for friction when weightless and not providing inputs.
        /// This is essentially how much their speed decreases per second.
        /// </summary>
        [AutoNetworkedField, DataField]
        public float BaseWeightlessFrictionNoInput = DefaultWeightlessFrictionNoInput;

        /// <summary>
        /// The negative velocity applied for friction when weightless and not standing on a grid or mapgrid
        /// </summary>
        [AutoNetworkedField, DataField]
        public float OffGridFriction = DefaultOffGridFriction;

        /// <summary>
        /// The movement speed modifier applied to a mob's total input velocity when weightless.
        /// </summary>
        [AutoNetworkedField, DataField]
        public float BaseWeightlessModifier = DefaultWeightlessModifier;

        /// <summary>
        /// The acceleration applied to mobs when moving and weightless.
        /// </summary>
        [AutoNetworkedField, DataField]
        public float BaseWeightlessAcceleration = DefaultWeightlessAcceleration;

        /*
         * Final values
         */

        [AutoNetworkedField, DataField]
        public float WeightlessAcceleration;

        [AutoNetworkedField, DataField]
        public float WeightlessModifier;

        [AutoNetworkedField, DataField]
        public float WeightlessFriction;

        [AutoNetworkedField, DataField]
        public float WeightlessFrictionNoInput;

        #endregion

        /// <summary>
        /// Minimum speed a mob has to be moving before applying movement friction.
        /// </summary>
        [DataField]
        public float MinimumFrictionSpeed = DefaultMinimumFrictionSpeed;

        /// <summary>
        /// The acceleration applied to mobs when moving.
        /// </summary>
        [AutoNetworkedField, DataField]
        public float Acceleration = DefaultAcceleration;

        /// <summary>
        /// The negative velocity applied for friction.
        /// </summary>
        [AutoNetworkedField, DataField]
        public float Friction = DefaultFriction;

        /// <summary>
        /// The negative velocity applied for friction.
        /// </summary>
        [AutoNetworkedField, DataField]
        public float? FrictionNoInput;

        [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
        public float BaseWalkSpeed { get; set; } = DefaultBaseWalkSpeed;

        [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
        public float BaseSprintSpeed { get; set; } = DefaultBaseSprintSpeed;

        [ViewVariables]
        public float CurrentWalkSpeed => WalkSpeedModifier * BaseWalkSpeed;
        [ViewVariables]
        public float CurrentSprintSpeed => SprintSpeedModifier * BaseSprintSpeed;
    }
}
