using Content.Server._FTL.ShipTracker.Systems;

namespace Content.Server._FTL.ShipTracker;

/// <summary>
/// This is used for tracking the damage on ships
/// </summary>
[RegisterComponent]
[Access(typeof(ShipTrackerSystem))]
public sealed class ShipTrackerComponent : Component
{
    /// <summary>
    /// How much hull does the ship have?
    /// </summary>
    [DataField("hullAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int HullAmount = 30;

    /// <summary>
    /// The maximum capacity of the hull
    /// </summary>
    [DataField("hullCapacity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int HullCapacity = 30;

    /// <summary>
    /// How many shield layers does the ship have?
    /// </summary>
    [DataField("shieldAmount"), ViewVariables(VVAccess.ReadOnly), Obsolete("Use the dedicated function in ShipTrackerSystem")]
    public int ShieldAmount = 1;

    /// <summary>
    /// The maximum capacity of the shields
    /// </summary>
    [DataField("shieldCapacity"), ViewVariables(VVAccess.ReadOnly), Obsolete("Use the dedicated function in ShipTrackerSystem")]
    public int ShieldCapacity = 1;

    /// <summary>
    /// How much time has passed since the last shield regen?
    /// </summary>
    public float TimeSinceLastShieldRegen = 0f;

    /// <summary>
    /// How much time has passed since an attack targeting the ship?
    /// </summary>
    public float TimeSinceLastAttack = 0f;

    /// <summary>
    /// How much time does it take to regen a shield layer?
    /// </summary>
    [DataField("shieldRegenTime"), ViewVariables(VVAccess.ReadWrite)]
    public float ShieldRegenTime = 5f;

    /// <summary>
    /// How much passive evasion does the ship have?
    /// </summary>
    [DataField("passiveEvasion")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float PassiveEvasion = 0.2f;

    /// <summary>
    /// The maximum capacity of the shields
    /// </summary>
    [DataField("faction")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string Faction = "IndependentShip";
}

[RegisterComponent]
public sealed class FTLActiveShipDestructionComponent : Component
{
    /// <summary>
    /// How much time has passed since destruction has begun?
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float TimePassed { get; set; } = 0f;

    /// <summary>
    /// How much time has passed since destruction has begun?
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float TimePassedSinceLastExplosion { get; set; } = 0f;

    /// <summary>
    /// If TimePassed is bigger than the limit, it will finish
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public readonly float ShipDestructionLimit = 15f;

    /// <summary>
    /// If TimePassed is bigger than the limit, it will spawn a minor explosion
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public readonly float ShipExplosionLimit = 1f;
}
