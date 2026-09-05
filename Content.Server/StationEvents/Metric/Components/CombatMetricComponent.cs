using Content.Server.StationEvents.Events;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Server.StationEvents.Metric.Components;

[RegisterComponent, Access(typeof(CombatMetricSystem))]
public sealed partial class CombatMetricComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 HostileScore = 10.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 FriendlyScore = 10.0f;

    /// <summary>
    ///   Cost per point of medical damage for friendly entities
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MedicalMultiplier = 0.05f;

    /// <summary>
    ///   Cost for friendlies who are in crit
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 CritScore = 10.0f;

    /// <summary>
    ///   Cost for friendlies who are dead
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 DeadScore = 20.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 maxItemThreat = 30.0f;

    /// <summary>
    ///   ItemThreat - evaluate based on item tags how powerful a player is
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, FixedPoint2> ItemThreat =
        new()
        {
            { "Taser", 2.0f },
            { "Sidearm", 2.0f },
            { "Rifle", 5.0f },
            { "SubMachineGun", 4.0f },
            { "Shotgun", 4.0f },
            { "Sniper", 6.0f },
            { "Launcher", 8.0f },
            { "LightMachineGun", 6.0f },
            { "HeavyMachineGun", 8.0f },
            { "HighRiskItem", 2.0f },
            { "CombatKnife", 1.0f },
            { "Knife", 1.0f },
            { "Handcuffs", 1.0f },
            { "Stunbaton", 3.0f },
            { "Armor", 1.5f },
            { "HeavyArmor", 3.0f },
            { "CombatHardsuit", 4.0f },
            { "HeavyCombatHardsuit", 6.0f },
            { "Grenade", 2.0f },
            { "Bomb", 2.0f },
            { "MagazinePistol", 0.5f },
            { "MagazinePistolHighCapacity", 1.0f },
            { "MagazinePistolSubMachineGun", 1.0f },
            { "MagazinePistolSubMachineGunTopMounted", 1.0f },
            { "MagazinePistolCaselessRifle", 0.5f },
            { "MagazineLightRifle", 1.0f },
            { "MagazineLightRifleBox", 2.0f },
            { "MagazineRifle", 1.5f },
            { "MagazineShotgun", 1.0f },
            { "MagazineMagnum", 1.0f },
            { "Hacking", 1.0f },
            { "Jetpack", 1.0f },
        };

}
