using Robust.Shared.Prototypes;

namespace Content.Server._FinalStand.GameTicking.Rules;

public enum WavePhase : byte
{
    Prep,
    Combat,
}

[DataDefinition]
public sealed partial class WaveEnemyConfig
{
    /// <summary>This config activates starting at this wave number (inclusive).</summary>
    [DataField]
    public int FromWave = 1;

    /// <summary>This config deactivates after this wave number (inclusive). Null = no upper bound.</summary>
    [DataField]
    public int? ToWave = null;

    [DataField]
    public List<EntProtoId> EnemyPool = new() { "MobXeno" };
}

[RegisterComponent, Access(typeof(WaveGameRuleSystem))]
public sealed partial class WaveGameRuleComponent : Component
{
    // ── YAML-configurable ────────────────────────────────────────────────────

    [DataField]
    public TimeSpan PrepDuration = TimeSpan.FromSeconds(600);

    [DataField]
    public TimeSpan MaxCombatDuration = TimeSpan.FromSeconds(1800);

    [DataField]
    public TimeSpan SpawnInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Wave director: ordered by FromWave ascending.
    /// The last entry whose FromWave ≤ current wave number (and ToWave is null or ≥ current wave) wins.
    /// </summary>
    [DataField]
    public List<WaveEnemyConfig> EnemyConfigs = new()
    {
        new WaveEnemyConfig { FromWave = 1, EnemyPool = new List<EntProtoId> { "MobXeno" } },
    };

    // ── Runtime state (not serialized) ──────────────────────────────────────

    public int WaveNumber = 1;
    public WavePhase Phase = WavePhase.Prep;
    public TimeSpan PhaseEndTime = TimeSpan.Zero;
    public TimeSpan NextSpawnTime = TimeSpan.Zero;
    public int EnemyTotalThisWave = 0;
    public int EnemiesSpawnedThisWave = 0;
    public int WavesCompleted = 0;
    public int TotalEnemiesKilled = 0;

    /// <summary>Live enemies spawned this wave. Cleaned up on death AND on entity deletion.</summary>
    public readonly HashSet<EntityUid> AliveEnemies = new();

    /// <summary>Cached list of WaveEnemySpawner entity UIDs populated at combat-phase start.</summary>
    public readonly List<EntityUid> SpawnerEntities = new();

    /// <summary>Throttles the combat heartbeat log to once every 5 seconds.</summary>
    public TimeSpan NextHeartbeatTime = TimeSpan.Zero;
}
