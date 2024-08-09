using Robust.Shared.GameStates;

namespace Content.Shared.Slippery;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlowedOverSlipComponent : Component
{

    [DataField, AutoNetworkedField]
    public float SlowModifier = 1f;

}
