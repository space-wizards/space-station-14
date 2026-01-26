using Robust.Shared.GameStates;
using Content.Shared.Damage;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Component to mark items that restore normal movement speed when held in-hand for entities with the impaired mobility trait.
/// The speed is automatically calculated to nullify the entity's speed penalty.
/// Should be used on items that act as mobility aids, such as canes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MobilityAidComponent : Component
{
    /// <summary>
    /// If true, this item is considered a makeshift mobility aid (not purpose-built, like a mop or pipe).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isMakeshift")]
    public bool IsMakeshift { get; set; } = false;

    /// <summary>
    /// How effective this aid is at restoring normal movement speed. Higher the number the more effective. Defaults to 1.0f.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("effectiveness")]
    public float Effectiveness { get; set; } = 1.0f;

    /// <summary>
    /// Chance (0.0–1.0) to trip when using a makeshift aid. Only applies if IsMakeshift is true. Defaults to 0.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("tripChance")]
    public float TripChance { get; set; } = 0.0f;

    /// <summary>
    /// Damage to deal when tripping with this makeshift aid. Only applies if IsMakeshift is true and TripChance triggers. Null means no damage.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("tripDamage")]
    public DamageSpecifier? TripDamage { get; set; } = null;
}
