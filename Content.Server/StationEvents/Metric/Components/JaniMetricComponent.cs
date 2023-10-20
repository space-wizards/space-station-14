using Content.Server.StationEvents.Events;
using Content.Shared.FixedPoint;

namespace Content.Server.StationEvents.Metric.Components;

[RegisterComponent, Access(typeof(JaniMetric))]
public sealed partial class JaniMetricComponent : Component
{
    /// <summary>
    ///   The cost of each puddle
    /// </summary>
    [DataField("puddles"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, FixedPoint2> Puddles =
        new()
        {
            { "Water", 2 },
            { "SpaceCleaner", 2.0f },

            { "Slime", 10.0f },
            { "Nutriment", 10.0f },
            { "Sugar", 10.0f },
            { "Ephedrine", 10.0f },
            { "Ale", 10.0f },
            { "Beer", 10.0f },

            { "Blood", 20.0f },
            { "SpaceDrugs", 30.0f },
            { "SpaceLube", 30.0f },
        };

    [DataField("puddleDefault"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PuddleDefault = 4.0f;

    /// <summary>
    ///   How many ml of the substance qualify as the point values described above
    /// </summary>
    [DataField("baselineQty"), ViewVariables(VVAccess.ReadWrite)]
    public float BaselineQty = 200.0f;

}
