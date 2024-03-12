using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This is used for making something have poor vision forever.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PermanentPoorVisionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int MinDamage = 0;
}

