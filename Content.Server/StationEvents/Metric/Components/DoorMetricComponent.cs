using Content.Server.StationEvents.Events;
using Content.Shared.FixedPoint;

namespace Content.Server.StationEvents.Metric.Components;

[RegisterComponent, Access(typeof(DoorMetric))]
public sealed partial class DoorMetricComponent : Component
{
    /// <summary>
    ///   Cost of an emagged door
    /// </summary>
    [DataField("emagCost"), ViewVariables(VVAccess.ReadWrite)]
    public float EmagCost = 10.0f;

    /// <summary>
    ///   Cost of a door with no power
    /// </summary>
    [DataField("powerCost"), ViewVariables(VVAccess.ReadWrite)]
    public float PowerCost = 5.0f;

    /// <summary>
    ///   Cost of a firedoor holding pressure
    /// </summary>
    [DataField("pressureCost"), ViewVariables(VVAccess.ReadWrite)]
    public float PressureCost = 10.0f;

    /// <summary>
    ///   Cost of a firedoor holding temperature
    /// </summary>
    [DataField("fireCost"), ViewVariables(VVAccess.ReadWrite)]
    public float FireCost = 20.0f;
}
