using Content.Server.Station.Systems;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Server.Station.Components;

[RegisterComponent, Friend(typeof(StationJobsSystem)), PublicAPI]
public sealed class StationJobsComponent : Component
{
    /// <summary>
    /// Total *round-start* jobs at station start.
    /// </summary>
    public int RoundStartTotalJobs = 0;

    /// <summary>
    /// Total *mid-round* jobs at station start.
    /// </summary>
    public int MidRoundTotalJobs = 0;

    /// <summary>
    /// Current total jobs.
    /// </summary>
    public int TotalJobs = 0;

    /// <summary>
    /// The percentage of jobs remaining.
    /// </summary>
    public float PercentJobsRemaining => TotalJobs / (float)MidRoundTotalJobs;

    /// <summary>
    /// The current list of jobs.
    /// </summary>
    /// <remarks>This should not be mutated or used directly, go through StationJobsSystem.</remarks>
    public Dictionary<string, int> JobList = new();
}
