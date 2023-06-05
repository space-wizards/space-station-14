using Content.Server.StationEvents.Events;
using Content.Shared.FixedPoint;

namespace Content.Server.StationEvents.Metric.Components;

[RegisterComponent, Access(typeof(AnomalyMetric))]
public sealed class AnomalyMetricComponent : Component
{
    /// <summary>
    ///   Cost of a growing anomaly
    /// </summary>
    [DataField("growingCost"), ViewVariables(VVAccess.ReadWrite)]
    public readonly float GrowingCost = 100.0f;

    /// <summary>
    ///   Cost of a dangerous anomaly
    /// </summary>
    [DataField("severityCost"), ViewVariables(VVAccess.ReadWrite)]
    public readonly float SeverityCost = 50.0f;

    /// <summary>
    ///   Cost of any anomaly
    /// </summary>
    [DataField("dangerCost"), ViewVariables(VVAccess.ReadWrite)]
    public readonly float BaseCost = 40.0f;
}
