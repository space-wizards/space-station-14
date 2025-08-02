using Content.Server.RoundStatistics.Systems;
using Content.Shared.RoundStatistics;
using Robust.Shared.Prototypes;

namespace Content.Server.RoundStatistics.Components;

/// <summary>
/// Holds round statistic data.
/// This exists as a component so the data can be attached to an entity,
/// allowing the data to persist through save/load operations.
/// </summary>
[RegisterComponent]
[Access(typeof(RoundEndStatisticsSystem))]
public sealed partial class RoundEndStatisticsComponent : Component
{
    /// <summary>
    /// Dictionary of statistic ProtoIds to counts.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<RoundStatisticPrototype>, int> Statistics = [];
}
