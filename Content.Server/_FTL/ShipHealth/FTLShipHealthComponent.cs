namespace Content.Server._FTL.ShipHealth;

/// <summary>
/// This is used for tracking the damage on ships
/// </summary>
[RegisterComponent]
[Access(typeof(FTLShipHealthSystem))]
public sealed class FTLShipHealthComponent : Component
{
    /// <summary>
    /// How much hull does the ship have?
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public int HullAmount = 15;

    /// <summary>
    /// The maximum capacity of the hull
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public int HullCapacity = 15;

    /// <summary>
    /// How many shield layers does the ship have?
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public int ShieldAmount = 1;
    /// <summary>
    /// The maximum capacity of the shields
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public int ShieldCapacity = 1;

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
    public float ShieldRegenTime = 3f;

    /// <summary>
    /// How much passive evasion does the ship have?
    /// </summary>
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
