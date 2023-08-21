using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Interaction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InteractionRangeComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("range")]
    public float Range = 1.5f;
}
