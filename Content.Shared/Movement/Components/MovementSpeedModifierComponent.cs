using Content.Shared.Movement.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Movement.Components
{
    [RegisterComponent]
    [NetworkedComponent, Friend(typeof(MovementSpeedModifierSystem))]
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
