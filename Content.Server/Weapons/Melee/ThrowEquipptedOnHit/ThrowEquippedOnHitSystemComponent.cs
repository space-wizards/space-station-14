
using Content.Shared.Inventory;
using Content.Shared.Whitelist;

namespace Content.Server.Weapons.Melee;

/// <summary>
/// Throw equipped items when this weapon hits an entity. Knock their socks off - literally!
/// </summary>
[RegisterComponent]
public sealed partial class ThrowEquippedOnHitComponent : Component
{
    /// <summary>
    /// What slots have a chance to get thrown?
    /// </summary>
    [DataField]
    public SlotFlags TargetSlots = SlotFlags.All & ~SlotFlags.INNERCLOTHING; // We don't want gloves of the freaky star...

    /// <summary>
    /// Whitelist for what equipment gets thrown.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist for what equipment gets thrown.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Chance to throw a piece of equipment when hit.
    /// </summary>
    [DataField]
    public float ThrowChance = 0.2f;

    /// <summary>
    /// Angle variance the equipment gets thrown at.
    /// </summary>
    [DataField]
    public double AngleVariance = Math.PI / 6.0f;
}
