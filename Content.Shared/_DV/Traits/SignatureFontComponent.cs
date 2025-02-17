using Robust.Shared.GameStates;

namespace Content.Shared.DV.Traits;

/// <summary>
/// Used to determine which font is used when signing paper, if nothing else overwrites it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SignatureFontComponent : Component
{
    [DataField("font"), ViewVariables(VVAccess.ReadWrite)]
    public string? Font;
}
