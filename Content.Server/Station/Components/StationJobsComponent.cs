using Content.Server.Station.Systems;
using JetBrains.Annotations;

namespace Content.Server.Station.Components;

/// <summary>
/// Stores information about a station's job selection.
/// </summary>
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
    /// <remarks>
    /// Null if MidRoundTotalJobs is zero. This is a NaN free API.
    /// </remarks>
    [ViewVariables]
    public float? PercentJobsRemaining => MidRoundTotalJobs > 0 ? TotalJobs / (float) MidRoundTotalJobs : null;

    /// <summary>
    /// The current list of jobs.
    /// </summary>
    /// <remarks>
    /// This should not be mutated or used directly unless you really know what you're doing, go through StationJobsSystem.
    /// </remarks>
    [ViewVariables] public Dictionary<string, uint?> JobList = new();

    /// <summary>
    /// The round-start list of jobs.
    /// </summary>
    /// <remarks>
    /// This should not be mutated, ever.
    /// </remarks>
    [ViewVariables] public Dictionary<string, uint?> RoundStartJobList = new();

    /// <summary>
    /// Overflow jobs that round-start can spawn infinitely many of.
    /// </summary>
    [ViewVariables] public HashSet<string> OverflowJobs = new();
}
