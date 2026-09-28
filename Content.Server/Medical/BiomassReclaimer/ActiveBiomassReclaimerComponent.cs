using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.BiomassReclaimer;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(BiomassReclaimerSystem))]
public sealed partial class ActiveBiomassReclaimerComponent : Component
{
    /// <summary>
    /// Biomass yield of the current job, excluding the machine's remainder.
    /// </summary>
    [ViewVariables]
    public float ExpectedYield;

    /// <summary>
    /// Reagents that can be spilled during this job.
    /// </summary>
    [ViewVariables]
    public Solution? BloodReagents;

    /// <summary>
    /// Entities that can be thrown during this job.
    /// </summary>
    [ViewVariables]
    public List<string> SpawnedEntities = [];

    /// <summary>
    /// When processing completes and biomass is produced.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ProcessingEndTime;

    /// <summary>
    /// When to next attempt to spill blood or throw an item.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextMessTime;

    /// <summary>
    /// When power loss paused this job. Null while processing.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? PowerLossTime;
}
