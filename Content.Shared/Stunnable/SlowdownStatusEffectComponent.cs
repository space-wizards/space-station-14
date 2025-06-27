using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlowdownStatusEffectComponent : Component
{
    [ViewVariables, DataField("sprintSpeedModifier"), AutoNetworkedField]
    public float SprintSpeedModifier = 0.5f;

    [ViewVariables, DataField("walkSpeedModifier"), AutoNetworkedField]
    public float WalkSpeedModifier = 0.5f;
}
