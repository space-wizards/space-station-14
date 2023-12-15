using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent]
public sealed partial class EntitySpawnAnomalyComponent : Component
{
    /// <summary>
    /// A list of entities that are random picked to be spawned on each pulse
    /// </summary>
    [DataField]
    public List<EntProtoId> Spawns = new();

    /// <summary>
    /// A list of entities that are random picked to be spawned when supercritical;
    /// </summary>
    [DataField]
    public List<EntProtoId> SuperCriticalSpawns = new();

    /// <summary>
    /// The maximum number of entities that spawn per pulse
    /// scales with severity.
    /// </summary>
    [DataField("maxSpawnAmount"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxSpawnAmount = 7;

    /// <summary>
    /// The maximum radius the entities will spawn in.
    /// Also governs the maximum reach of flesh tiles
    /// scales with stability
    /// </summary>
    [DataField("spawnRange"), ViewVariables(VVAccess.ReadWrite)]
    public float SpawnRange = 5f;

    /// <summary>
    /// Whether or not anomaly spawns entities on Pulse
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool SpawnOnPulse = true;

    /// <summary>
    /// Whether or not anomaly spawns entities on SuperCritical
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool SpawnOnSuperCritical = true;

    /// <summary>
    /// Whether or not anomaly spawns entities on StabilityChanged
    /// The idea was to spawn entities either on Pulse/Supercritical OR StabilityChanged
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool SpawnOnStabilityChanged = false;
}
