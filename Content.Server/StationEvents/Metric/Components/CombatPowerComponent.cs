using Content.Server.StationEvents.Events;
using Content.Shared.FixedPoint;

namespace Content.Server.StationEvents.Metric.Components;

/// <summary>
///   Some entities (such as dragons) are just more dangerous
/// </summary>
[RegisterComponent, Access(typeof(CombatMetricSystem))]
public sealed partial class CombatPowerComponent : Component
{
    /// <summary>
    ///   Threat, expressed as a multiplier (1x is similar to a single player)
    /// </summary>
    [DataField("threat"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Threat = 1.0f;
}
