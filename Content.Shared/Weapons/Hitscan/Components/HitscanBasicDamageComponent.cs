using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Hitscan entities that have this component will do the damage specified to hit targets (Who didn't reflect it).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanBasicDamageComponent : Component
{
    /// <summary>
    /// How much damage the hitscan weapon will do when hitting a target.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage;
}
