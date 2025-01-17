using Content.Shared.Eye;

namespace Content.Shared.Revenant.Components;

// TODO separate component
// Visibility, collision, and slowdown should be separate components
/// <summary>
/// Makes the target solid, visible, and applies a slowdown.
/// Meant to be used in conjunction with statusEffectSystem
/// </summary>
[RegisterComponent]
public sealed partial class CorporealComponent : Component
{
    /// <summary>
    /// The debuff applied when the component is present.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MovementSpeedDebuff = 0.66f;

    [DataField]
    public bool MadeVisible;
}
