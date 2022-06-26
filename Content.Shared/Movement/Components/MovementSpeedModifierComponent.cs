using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components
{
    [RegisterComponent]
    [NetworkedComponent, Access(typeof(MovementSpeedModifierSystem))]
    public sealed class MovementSpeedModifierComponent : Component
    {
        public const float DefaultBaseWalkSpeed = 3.0f;
        public const float DefaultBaseSprintSpeed = 5.0f;

        [ViewVariables]
        public float WalkSpeedModifier = 1.0f;

        [ViewVariables]
        public float SprintSpeedModifier = 1.0f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseWalkSpeedVV
        {
            get => BaseWalkSpeed;
            set
            {
                BaseWalkSpeed = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseSprintSpeedVV
        {
            get => BaseSprintSpeed;
            set
            {
                BaseSprintSpeed = value;
                Dirty();
            }
        }

        [DataField("baseWalkSpeed")]
        public float BaseWalkSpeed { get; set; } = DefaultBaseWalkSpeed;

        [DataField("baseSprintSpeed")]
        public float BaseSprintSpeed { get; set; } = DefaultBaseSprintSpeed;

        [ViewVariables]
        public float CurrentWalkSpeed => WalkSpeedModifier * BaseWalkSpeed;
        [ViewVariables]
        public float CurrentSprintSpeed => SprintSpeedModifier * BaseSprintSpeed;
    }
}
