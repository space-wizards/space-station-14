using Content.Server.StationEvents.Events;
using Content.Shared.FixedPoint;

namespace Content.Server.StationEvents.Metric.Components;

[RegisterComponent, Access(typeof(CombatMetric))]
public sealed class CombatMetricComponent : Component
{
    [DataField("hostileScore"), ViewVariables(VVAccess.ReadWrite)]
    public readonly FixedPoint2 HostileScore = 10.0f;

    [DataField("nukieScore"), ViewVariables(VVAccess.ReadWrite)]
    public readonly FixedPoint2 NukieScore = 30.0f;

    [DataField("zombieScore"), ViewVariables(VVAccess.ReadWrite)]
    public readonly FixedPoint2 ZombieScore = 10.0f;

    [DataField("friendlyScore"), ViewVariables(VVAccess.ReadWrite)]
    public readonly FixedPoint2 FriendlyScore = 10.0f;

    /// <summary>
    ///     Cost per point of medical damage for friendly entities
    /// </summary>
    [DataField("medicalMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public readonly FixedPoint2 MedicalMultiplier = 0.2f;

    /// <summary>
    ///     Cost for friendlies who are in crit
    /// </summary>
    [DataField("critScore"), ViewVariables(VVAccess.ReadWrite)]
    public readonly FixedPoint2 CritScore = 50.0f;

    /// <summary>
    ///     Cost for friendlies who are dead
    /// </summary>
    [DataField("deadScore"), ViewVariables(VVAccess.ReadWrite)]
    public readonly FixedPoint2 DeadScore = 100.0f;

    // [DataField("secScore), ViewVariables(VVAccess.ReadWrite)]
    // public readonly FixedPoint2 SecScore = 10.0f;

}
