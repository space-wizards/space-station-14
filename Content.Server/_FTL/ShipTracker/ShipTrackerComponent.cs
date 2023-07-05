namespace Content.Server._FTL.ShipHealth;

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
    [DataField("shieldAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int ShieldAmount = 1;

    /// <summary>
    /// The maximum capacity of the shields
    /// </summary>
    [DataField("shieldCapacity")]
    [ViewVariables(VVAccess.ReadWrite)]
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
    public float ShieldRegenTime = 5f;

    /// <summary>
    /// How much passive evasion does the ship have?
    /// </summary>
    [DataField("passiveEvasion")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float PassiveEvasion = 0.2f;
}

[RegisterComponent]
public sealed class FTLActiveShipDestructionComponent : Component
{
    /// <summary>
    /// How much time has passed?
    /// </summary>
    public float TimePassed = 0f;
}
