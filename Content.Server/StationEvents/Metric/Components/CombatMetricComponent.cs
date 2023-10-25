using Content.Server.StationEvents.Events;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Server.StationEvents.Metric.Components;

[RegisterComponent, Access(typeof(CombatMetricSystem))]
public sealed partial class CombatMetricComponent : Component
{
    [DataField("hostileScore"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 HostileScore = 10.0f;

    [DataField("friendlyScore"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 FriendlyScore = 10.0f;

    /// <summary>
    ///   Cost per point of medical damage for friendly entities
    /// </summary>
    [DataField("medicalMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MedicalMultiplier = 0.05f;

    /// <summary>
    ///   Cost for friendlies who are in crit
    /// </summary>
    [DataField("critScore"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 CritScore = 10.0f;

    /// <summary>
    ///   Cost for friendlies who are dead
    /// </summary>
    [DataField("deadScore"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 DeadScore = 20.0f;

    // [DataField("secScore), ViewVariables(VVAccess.ReadWrite)]
    // public readonly FixedPoint2 SecScore = 10.0f;

    [DataField("maxItemThreat"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 maxItemThreat = 15.0f;

    /// <summary>
    ///   ItemThreat - evaluate based on item tags how powerful a player is
    /// </summary>
    [DataField("itemThreat", customTypeSerializer: typeof(DictionarySerializer<string, FixedPoint2>)), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, FixedPoint2> itemThreat =
        new()
        {
            { "Taser", 2.0f },
            { "Sidearm", 2.0f },
            { "Rifle", 5.0f },
            { "HighRiskItem", 2.0f },
            { "CombatKnife", 1.0f },
            { "Knife", 1.0f },
            { "Grenade", 2.0f },
            { "Bomb", 2.0f },
            { "MagazinePistol", 0.5f },
            { "Hacking", 1.0f },
            { "Jetpack", 1.0f },
        };

}
