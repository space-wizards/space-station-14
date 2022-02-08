using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Movement.Components;

[NetworkedComponent, RegisterComponent]
public class SlowContactsComponent : Component
{
    [ViewVariables, DataField("walkSpeedModifier")]
    public float WalkSpeedModifier { get; private set; } = 1.0f;

    [ViewVariables, DataField("sprintSpeedModifier")]
    public float SprintSpeedModifier { get; private set; } = 1.0f;
}
