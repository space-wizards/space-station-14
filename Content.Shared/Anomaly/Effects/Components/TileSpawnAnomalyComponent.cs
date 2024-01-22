using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent, Access(typeof(SharedTileAnomalySystem))]
public sealed partial class TileSpawnAnomalyComponent : Component
{
    /// <summary>
    /// All types of floors spawns with their settings
    /// </summary>
    [DataField]
    public List<TileSpawnSettingsEntry> Entries = new();
}

[DataDefinition, Serializable]
public partial record struct TileSpawnSettingsEntry()
{
    /// <summary>
    /// The tile that is spawned by the anomaly's effect
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Floor { get; set; } = default!;

    /// <summary>
    /// The minimum number of tiles that spawn per pulse
    /// </summary>
    [DataField]
    public int MinAmount { get; set; } = 0;

    /// <summary>
    /// The maximum number of tiles that spawn per pulse
    /// scales with severity.
    /// </summary>
    [DataField]
    public int MaxAmount { get; set; } = 1;

    /// <summary>
    /// The distance from the anomaly in which the tiles will not appear
    /// </summary>
    [DataField]
    public float MinRange { get; set; } = 0f;

    /// <summary>
    /// The maximum radius the tiles will spawn in.
    /// </summary>
    [DataField]
    public float MaxRange { get; set; } = 1f;

    /// <summary>
    /// Whether or not anomaly spawns tiles on Pulse
    /// </summary>
    [DataField]
    public bool SpawnOnPulse { get; set; } = false;

    /// <summary>
    /// Whether or not anomaly spawns tiles on SuperCritical
    /// </summary>
    [DataField]
    public bool SpawnOnSuperCritical { get; set; } = false;

    /// <summary>
    /// Whether or not anomaly spawns tiles on StabilityChanged
    /// </summary>
    [DataField]
    public bool SpawnOnStabilityChanged { get; set; } = false;

    /// <summary>
    /// Whether or not anomaly spawns tiles on StabilityChanged
    /// </summary>
    [DataField]
    public bool SpawnOnSeverityChanged { get; set; } = false;
}
