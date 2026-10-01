using Content.Shared.Chemistry.Components;
using Content.Shared.Storage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.BiomassReclaimer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(BiomassReclaimerSystem))]
public sealed partial class ActiveBiomassReclaimerComponent : Component
{
    /// <summary>
    /// The reagents that will be spilled while processing a mob.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Solution? BloodReagents;

    /// <summary>
    /// Amount of biomass that the entity being processed will yield.
    /// This is calculated from YieldPerUnitMass.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float ExpectedYield;

    /// <summary>
    /// When this time is reached, there is a chance for the reclaimer to either spill blood or throw an item.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan NextMessTime;

    /// <summary>
    /// When processing was paused by a power loss. Null while processing is running.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan? PowerLossTime;

    /// <summary>
    /// When this time is reached, processing is completed and biomass is produced.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan ProcessingEndTime;

    /// <summary>
    /// Entities that can be randomly spawned while processing.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<EntitySpawnEntry> SpawnedEntities = [];
}
