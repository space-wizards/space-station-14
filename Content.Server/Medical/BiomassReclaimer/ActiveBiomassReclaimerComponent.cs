using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.BiomassReclaimer;

/// <summary>
/// State of the current processing job, retained while the reclaimer is unpowered.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ServerBiomassReclaimerSystem))]
public sealed partial class ActiveBiomassReclaimerComponent : Component
{
    /// <summary>
    /// Reagents that can be spilled during this job.
    /// </summary>
    [ViewVariables]
    public Solution? BloodReagents;

    /// <summary>
    /// Biomass yield of the current job, excluding the machine's remainder.
    /// </summary>
    [ViewVariables]
    public float ExpectedYield;

    /// <summary>
    /// Time of the next attempt to spill blood or throw an item.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextMessTime;

    /// <summary>
    /// Time at which power loss paused processing. Null when the job is not paused by power loss.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? PowerLossTime;

    /// <summary>
    /// Scheduled completion time, extended when processing resumes after power loss.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ProcessingEndTime;

    /// <summary>
    /// Prototype IDs selected for this job, used to spawn entities that can be thrown while processing.
    /// </summary>
    [ViewVariables]
    public List<string> SpawnedEntities = [];
}
