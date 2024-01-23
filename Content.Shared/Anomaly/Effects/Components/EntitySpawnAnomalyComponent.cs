using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent, Access(typeof(SharedEntityAnomalySystem))]
public sealed partial class EntitySpawnAnomalyComponent : Component
{
    /// <summary>
    /// All types of entity spawns with their settings
    /// </summary>
    [DataField]
    public List<EntitySpawnSettingsEntry> Entries = new();
}

[DataDefinition, Serializable]
public partial record struct EntitySpawnSettingsEntry()
{
    /// <summary>
    /// A list of entities that are random picked to be spawned on each pulse
    /// </summary>
    [DataField]
    public List<EntProtoId> Spawns { get; set; } = new();

    /// <summary>
    /// The minimum number of entities that spawn per pulse
    /// </summary>
    [DataField]
    public int MinAmount { get; set; } = 0;

    /// <summary>
    /// The maximum number of entities that spawn per pulse
    /// scales with severity.
    /// </summary>
    [DataField]
    public int MaxAmount { get; set; } = 1;

    /// <summary>
    /// The distance from the anomaly in which the entities will not appear
    /// </summary>
    [DataField]
    public float MinRange { get; set; } = 0f;

    /// <summary>
    /// The maximum radius the entities will spawn in.
    /// </summary>
    [DataField]
    public float MaxRange { get; set; } = 1f;

    /// <summary>
    /// Whether or not anomaly spawns entities on Pulse
    /// </summary>
    [DataField]
    public bool SpawnOnPulse { get; set; } = false;

    /// <summary>
    /// Whether or not anomaly spawns entities on SuperCritical
    /// </summary>
    [DataField]
    public bool SpawnOnSuperCritical { get; set; } = false;

    /// <summary>
    /// Whether or not anomaly spawns entities when destroyed
    /// </summary>
    [DataField]
    public bool SpawnOnShutdown { get; set; } = false;

    /// <summary>
    /// Whether or not anomaly spawns entities on StabilityChanged
    /// </summary>
    [DataField]
    public bool SpawnOnStabilityChanged { get; set; } = false;

    /// <summary>
    /// Whether or not anomaly spawns entities on SeverityChanged
    /// </summary>
    [DataField]
    public bool SpawnOnSeverityChanged { get; set; } = false;
}
