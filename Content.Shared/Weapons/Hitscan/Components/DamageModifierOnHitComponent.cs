using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Scales the damage of hitscan weapons based on what entites they have hit in their path so far.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DamageModifierOnHitComponent : Component
{
    /// <summary>
    /// Current amount to scale the damage by.
    /// </summary>
    [DataField]
    public float DamageScaler = 1.0f;
}
