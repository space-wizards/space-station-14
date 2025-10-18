using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared.GhostTypes;

/// <summary>
/// Added to the Mind of an entity by the StoreDamageTakenOnMindSystem, allowing storage of the damage values their body had.
/// </summary>
[RegisterComponent]
public sealed partial class LastBodyDamageComponent : Component
{
    /// <summary>
    /// Entity damage stored by the StoreDamageTakenOnMind, indexed by the DamageableSystem.
    /// </summary>
    [DataField]
    public Dictionary<string, FixedPoint2>? DamagePerGroup;

    /// <summary>
    /// Collection of possible damage types, stored by the StoreDamageTakenOnMind.
    /// </summary>
    [DataField]
    public DamageSpecifier? Damage;
}
