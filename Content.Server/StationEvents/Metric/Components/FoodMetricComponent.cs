using Content.Server.StationEvents.Events;
using Content.Shared.FixedPoint;

namespace Content.Server.StationEvents.Metric.Components;

[RegisterComponent, Access(typeof(FoodMetric))]
public sealed partial class FoodMetricComponent : Component
{
    [DataField("thirstScore"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 ThirstScore = 2.0f;

    [DataField("parchedScore"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 ParchedScore = 5.0f;

    [DataField("peckishScore"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PeckishScore = 2.0f;

    [DataField("starvingScore"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 StarvingScore = 5.0f;
}
