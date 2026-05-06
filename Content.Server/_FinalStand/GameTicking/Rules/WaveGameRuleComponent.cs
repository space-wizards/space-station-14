using Robust.Shared.Audio;
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
    [DataField]
    public int FromWave = 1;
    [DataField]
    public int? ToWave = null;

    [DataField]
    public List<EntProtoId> EnemyPool = new() { "MobXeno" };
}

[RegisterComponent, Access(typeof(WaveGameRuleSystem))]
public sealed partial class WaveGameRuleComponent : Component
{
    // adjustable timers

    [DataField]
    public TimeSpan PrepDuration = TimeSpan.FromSeconds(600);

    [DataField]
    public TimeSpan MaxCombatDuration = TimeSpan.FromSeconds(1800);

    [DataField]
    public TimeSpan SpawnInterval = TimeSpan.FromSeconds(2);

    [DataField]
    public List<WaveEnemyConfig> EnemyConfigs = new()
    {
        new WaveEnemyConfig { FromWave = 1, EnemyPool = new List<EntProtoId> { "MobXeno" } },
    };

    [DataField]
    public SoundSpecifier? WaveStartSound = new SoundPathSpecifier("/Audio/_FinalStand/WaveEvents/wave_start.ogg");

    [DataField]
    public SoundSpecifier? WaveEndSound = new SoundPathSpecifier("/Audio/_FinalStand/WaveEvents/wave_end.ogg");

    // ── Runtime state

    public int WaveNumber = 1;
    public WavePhase Phase = WavePhase.Prep;
    public TimeSpan PhaseEndTime = TimeSpan.Zero;
    public TimeSpan NextSpawnTime = TimeSpan.Zero;
    public int EnemyTotalThisWave = 0;
    public int EnemiesSpawnedThisWave = 0;
    public int WavesCompleted = 0;
    public int TotalEnemiesKilled = 0;

    public readonly HashSet<EntityUid> AliveEnemies = new();
    public readonly List<EntityUid> SpawnerEntities = new();
    public EntityUid CCCEntity = EntityUid.Invalid;
    public TimeSpan NextHeartbeatTime = TimeSpan.Zero;
}
