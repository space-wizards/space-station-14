namespace Content.Server.StationEvents.Metric.Components;

/// <summary>
///   Defines basic data for a station metric
/// </summary>
[RegisterComponent]
public sealed partial class ChaosMetricComponent : Component
{

}

/// <summary>
///   Raised to request metrics to calculate and sum their statistics
/// </summary>
[ByRefEvent]
public record struct CalculateChaosEvent(ChaosMetrics Metrics);

public record struct AddMetric();
