using Content.Server.Station.Systems;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Station.Components;

/// <summary>
/// Stores information about a station's job selection.
/// </summary>
[RegisterComponent, Access(typeof(StationJobsSystem)), PublicAPI]
public sealed class StationJobsComponent : Component
{
    /// <summary>
    /// Total *round-start* jobs at station start.
    /// </summary>
    [DataField("roundStartTotalJobs")] public int RoundStartTotalJobs;

    /// <summary>
    /// Total *mid-round* jobs at station start.
    /// </summary>
    [DataField("midRoundTotalJobs")] public int MidRoundTotalJobs;

    /// <summary>
    /// Current total jobs.
    /// </summary>
    [DataField("totalJobs")] public int TotalJobs;

    /// <summary>
    /// Station is running on extended access.
    /// </summary>
    [DataField("extendedAccess")] public bool ExtendedAccess;

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
    [DataField("jobList", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<uint?, JobPrototype>))]
    public Dictionary<string, uint?> JobList = new();

    /// <summary>
    /// The round-start list of jobs.
    /// </summary>
    /// <remarks>
    /// This should not be mutated, ever.
    /// </remarks>
    [DataField("roundStartJobList", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<uint?, JobPrototype>))]
    public Dictionary<string, uint?> RoundStartJobList = new();

    /// <summary>
    /// Overflow jobs that round-start can spawn infinitely many of.
    /// </summary>
    [DataField("overflowJobs", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<JobPrototype>))] public HashSet<string> OverflowJobs = new();
}
