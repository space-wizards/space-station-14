namespace Content.Shared._Impstation.Weapons.Ranged.Events;

/// <summary>
/// raised on a gun when it calculates what the next ammo recharge delay should be.
/// </summary>
[ByRefEvent]
public record struct GetAmmoRechargeTimeEvent
{
    public float Time;
}

