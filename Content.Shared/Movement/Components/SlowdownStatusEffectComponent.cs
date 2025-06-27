using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlowdownStatusEffectComponent : Component
{
    [ViewVariables, DataField("sprintSpeedModifier"), AutoNetworkedField]
    public float SprintSpeedModifier = 0.5f;

    [ViewVariables, DataField("walkSpeedModifier"), AutoNetworkedField]
    public float WalkSpeedModifier = 0.5f;
}
