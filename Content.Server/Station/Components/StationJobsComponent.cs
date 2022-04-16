using Content.Server.Station.Systems;
using JetBrains.Annotations;

namespace Content.Server.Station.Components;

[RegisterComponent, Friend(typeof(StationJobsSystem)), PublicAPI]
public sealed class StationJobsComponent : Component
{
    /// <summary>
    /// Total *round-start* jobs at station start.
    /// </summary>
    [ViewVariables]
    public int RoundStartTotalJobs = 0;

    /// <summary>
    /// Total *mid-round* jobs at station start.
    /// </summary>
    [ViewVariables]
    public int MidRoundTotalJobs = 0;

    /// <summary>
    /// Current total jobs.
    /// </summary>
    [ViewVariables]
    public int TotalJobs = 0;

    /// <summary>
    /// The percentage of jobs remaining.
    /// </summary>
    [ViewVariables]
    public float PercentJobsRemaining => TotalJobs / (float)MidRoundTotalJobs;

    /// <summary>
    /// The current list of jobs.
    /// </summary>
    /// <remarks>This should not be mutated or used directly, go through StationJobsSystem.</remarks>
    [ViewVariables]
    public Dictionary<string, int> JobList = new();

    /// <summary>
    /// Overflow jobs that roundstart can spawn infinitely many of.
    /// </summary>
    [ViewVariables]
    public HashSet<string> OverflowJobs = new();
}
