using Content.Server.Station.Systems;
using JetBrains.Annotations;

namespace Content.Server.Station.Components;

[RegisterComponent, Friend(typeof(StationJobsSystem)), PublicAPI]
public sealed class StationJobsComponent : Component
{
    /// <summary>
    /// Total *round-start* jobs at station start.
    /// </summary>
    [ViewVariables] public int RoundStartTotalJobs;

    /// <summary>
    /// Total *mid-round* jobs at station start.
    /// </summary>
    [ViewVariables] public int MidRoundTotalJobs;

    /// <summary>
    /// Current total jobs.
    /// </summary>
    [ViewVariables] public int TotalJobs;

    /// <summary>
    /// The percentage of jobs remaining.
    /// </summary>
    [ViewVariables]
    public float PercentJobsRemaining => TotalJobs / (float) MidRoundTotalJobs;

    /// <summary>
    /// The current list of jobs.
    /// </summary>
    /// <remarks>
    /// This should not be mutated or used directly unless you really know what you're doing, go through StationJobsSystem.
    /// </remarks>
    [ViewVariables] public Dictionary<string, uint?> JobList = new();

    /// <summary>
    /// Overflow jobs that round-start can spawn infinitely many of.
    /// </summary>
    [ViewVariables] public HashSet<string> OverflowJobs = new();
}
