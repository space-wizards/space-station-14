using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[NetworkedComponent, RegisterComponent]
public sealed class SlowContactsComponent : Component
{
    [ViewVariables, DataField("walkSpeedModifier")]
    public float WalkSpeedModifier { get; private set; } = 1.0f;

    [ViewVariables, DataField("sprintSpeedModifier")]
    public float SprintSpeedModifier { get; private set; } = 1.0f;
}
