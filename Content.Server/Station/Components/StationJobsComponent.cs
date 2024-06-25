using System.Linq;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Station.Components;

/// <summary>
/// Stores information about a station's job selection.
/// </summary>
[RegisterComponent, Access(typeof(StationJobsSystem)), PublicAPI]
public sealed partial class StationJobsComponent : Component
{
    /// <summary>
    /// Total *mid-round* jobs at station start.
    /// This is inferred automatically from <see cref="SetupAvailableJobs"/>.
    /// </summary>
    [ViewVariables] public int MidRoundTotalJobs;

    /// <summary>
    /// Current total jobs.
    /// </summary>
    [DataField] public int TotalJobs;

    /// <summary>
    /// Station is running on extended access.
    /// </summary>
    [DataField] public bool ExtendedAccess;

    /// <summary>
    /// If there are less than or equal this amount of players in the game at round start,
    /// people get extended access levels from job prototypes.
    /// </summary>
    /// <remarks>
    /// Set to -1 to disable extended access.
    /// </remarks>
    [DataField]
    public int ExtendedAccessThreshold { get; set; } = 15;

    /// <summary>
    /// The percentage of jobs remaining.
    /// </summary>
    /// <remarks>
    /// Null if MidRoundTotalJobs is zero. This is a NaN free API.
    /// </remarks>
    [ViewVariables]
    public float? PercentJobsRemaining => MidRoundTotalJobs > 0 ? TotalJobs / (float) MidRoundTotalJobs : null;

    /// <summary>
    /// The current list of jobs of available jobs. Null implies that is no limit.
    /// </summary>
    /// <remarks>
    /// This should not be mutated or used directly unless you really know what you're doing, go through StationJobsSystem.
    /// </remarks>
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int?> JobList = new();

    /// <summary>
    /// Overflow jobs that round-start can spawn infinitely many of.
    /// This is inferred automatically from <see cref="SetupAvailableJobs"/>.
    /// </summary>
    [ViewVariables]
    public IReadOnlySet<ProtoId<JobPrototype>> OverflowJobs = default!;

    /// <summary>
    /// A dictionary relating a NetUserId to the jobs they have on station.
    /// An OOC way to track where job slots have gone.
    /// </summary>
    [DataField]
    public Dictionary<NetUserId, List<ProtoId<JobPrototype>>> PlayerJobs = new();

    /// <summary>
    /// Mapping of jobs to an int[2] array that specifies jobs available at round start, and midround.
    /// Negative values implies that there is no limit.
    /// </summary>
    [DataField("availableJobs", required: true)]
    public Dictionary<ProtoId<JobPrototype>, int[]> SetupAvailableJobs = default!;
}
