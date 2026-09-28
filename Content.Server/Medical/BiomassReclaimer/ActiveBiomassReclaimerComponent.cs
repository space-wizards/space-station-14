using Content.Shared.Chemistry.Components;
using Content.Shared.Storage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.BiomassReclaimer;

/// <summary>
/// The current processing state, kept when the reclaimer loses power.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ServerBiomassReclaimerSystem))]
public sealed partial class ActiveBiomassReclaimerComponent : Component
{
    /// <summary>
    /// The reagents that will be spilled while processing a mob.
    /// </summary>
    [ViewVariables]
    public Solution? BloodReagents;

    /// <summary>
    /// Amount of biomass that the mob being processed will yield.
    /// This is calculated from the YieldPerUnitMass.
    /// </summary>
    [ViewVariables]
    public float ExpectedYield;

    /// <summary>
    /// This gets set for each mob it processes.
    /// When this time is reached, there is a chance for the reclaimer to either spill blood or throw an item.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextMessTime;

    /// <summary>
    /// When processing was paused by a power loss. Null while processing is running.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? PowerLossTime;

    /// <summary>
    /// This gets set for each mob it processes.
    /// When this time is reached, spit out biomass.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ProcessingEndTime;

    /// <summary>
    /// Entities that can be randomly spawned while processing a mob.
    /// </summary>
    [ViewVariables]
    public List<EntitySpawnEntry> SpawnedEntities = [];
}
