using Robust.Shared.GameStates;

namespace Content.Shared.Signature;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SignatureComponent : Component
{
    [DataField, AutoNetworkedField]
    public SignatureData? Data;
}
