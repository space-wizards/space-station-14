using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Components;

[NetworkedComponent, RegisterComponent]
public sealed class SlowContactsComponent : Component
{
    [DataField("walkSpeedModifier")]
    public float WalkSpeedModifier { get; set; } = 1.0f;

    [DataField("sprintSpeedModifier")]
    public float SprintSpeedModifier { get; set; } = 1.0f;

    [DataField("ignoreWhitelist")]
    public EntityWhitelist? IgnoreWhitelist;
}

[Serializable, NetSerializable]
public sealed class SlowContactsComponentState : ComponentState
{
    public readonly float WalkSpeedModifier;

    public readonly float SprintSpeedModifier;

    public SlowContactsComponentState(float walkSpeedModifier, float sprintSpeedModifier)
    {
        WalkSpeedModifier = walkSpeedModifier;
        SprintSpeedModifier = sprintSpeedModifier;
    }
}
