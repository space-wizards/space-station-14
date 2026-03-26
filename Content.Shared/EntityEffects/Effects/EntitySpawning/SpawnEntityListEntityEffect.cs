using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SpawnEntityList : EntityEffectBase<SpawnEntityList>
{
    /// <summary>
    /// All types of entity spawns with their settings
    /// </summary>
    [DataField]
    public List<EntityListSpawnSettingsEntry> Entries = new();

    /// <summary>
    /// Manually adjust scaling
    /// </summary>
    [DataField]
    public float ResizeScale = 1.0f;

    /// <summary>
    /// If true, the spawned entities will "jitter" around as they are spawned in
    /// Not usually great behavior, but does sometimes look cool.
    /// </summary>
    [DataField]
    public bool AllowMessyPrediction = false;
}

[DataRecord]
public partial record struct EntityListSpawnSettingsEntry()
{
    /// <summary>
    /// A list of entities that are random picked to be spawned on each pulse
    /// </summary>
    public List<EntProtoId> Spawns { get; set; } = new();

    public EntityListSpawnSettings Settings { get; set; } = new();
}

[DataRecord]
public partial record struct EntityListSpawnSettings()
{
    /// <summary>
    /// should entities block spawning?
    /// </summary>
    public bool CanSpawnOnEntities { get; set; } = false;

    /// <summary>
    /// The minimum number of entities that spawn
    /// </summary>
    public int MinAmount { get; set; } = 0;

    /// <summary>
    /// The maximum number of entities that spawn
    /// scales with severity.
    /// </summary>
    public int MaxAmount { get; set; } = 1;

    /// <summary>
    /// The distance from the initial entity in which the entities will not appear
    /// </summary>
    public float MinRange { get; set; } = 0f;

    /// <summary>
    /// The maximum radius the entities will spawn in.
    /// </summary>
    public float MaxRange { get; set; } = 1f;
}
