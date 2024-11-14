using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Component to handle non-breathing gas interactions.
/// Detects gasses around entities and applies effects. (this is currently for damage to borgs but ¯\_(ツ)_/¯)
/// </summary>
[RegisterComponent]
public sealed partial class InWaterComponent : Component
{

    // amount of gas needed to trigger effect
    [DataField("waterThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public float WaterThreshold = 0.1f;

    /// <summary>
    ///   Whether the entity is damaged by water.
    ///   By default things are not
    /// </summary>
    [DataField("damagedByWater"), ViewVariables(VVAccess.ReadWrite)]
    public bool DamagedByWater = false;

    /// Damage caused by water contact
    [DataField("damage"), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;

    ///<summary>
    /// Prevents gibbing from water damage, same purpose as the barotrauma one
    /// </summary>
    [DataField("maxDamage"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxDamage = 200;

    /// <summary>
    /// Used to track when damage starts/stops. Used in logs.
    /// </summary>
    public bool TakingDamage = false;
}
