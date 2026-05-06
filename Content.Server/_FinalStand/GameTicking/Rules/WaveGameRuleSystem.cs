using Content.Server._FinalStand.Spawners;
using Content.Server._FinalStand.Station;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Shared._FinalStand.WaveHud;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._FinalStand.GameTicking.Rules;

public sealed partial class WaveGameRuleSystem : GameRuleSystem<WaveGameRuleComponent>
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        // mainly just a guard again admin deleting mobs. if they don't get the mobstatechanged event, they won't be counted as dead and the wave won't end.
        SubscribeLocalEvent<WaveSpawnedTagComponent, ComponentShutdown>(OnWaveEnemyShutdown);
    }

    protected override void Started(EntityUid uid, WaveGameRuleComponent comp,
        GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        StartPrepPhase(uid, comp);
    }

    protected override void ActiveTick(EntityUid uid, WaveGameRuleComponent comp,
        GameRuleComponent gameRule, float frameTime)
    {
        var now = Timing.CurTime;
        switch (comp.Phase)
        {
            case WavePhase.Prep:
                if (now >= comp.PhaseEndTime)
                {
                    Log.Info($"[WaveGameRule] Prep timer expired for wave {comp.WaveNumber}, auto-starting.");
                    StartCombatPhase(uid, comp);
                }
                break;

            case WavePhase.Combat:
                if (now >= comp.PhaseEndTime)
                {
                    Log.Warning($"[WaveGameRule] Wave {comp.WaveNumber} fallback timer expired. Forcing end.");
                    // TODO: delete or quarantine stuck enemies here.
                    EndCombatPhase(uid, comp);
                    break;
                }

                if (comp.EnemiesSpawnedThisWave < comp.EnemyTotalThisWave && now >= comp.NextSpawnTime)
                    SpawnNextBatch(uid, comp);

                if (now >= comp.NextHeartbeatTime)
                {
                    var timeLeft = comp.PhaseEndTime - now;
                    Log.Info($"[WaveGameRule] Wave {comp.WaveNumber} | " +
                             $"spawned {comp.EnemiesSpawnedThisWave}/{comp.EnemyTotalThisWave} | " +
                             $"alive {comp.AliveEnemies.Count} | " +
                             $"fallback in {timeLeft.TotalSeconds:F0}s");
                    comp.NextHeartbeatTime = now + TimeSpan.FromSeconds(5);
                }
                break;
        }
    }

    protected override void AppendRoundEndText(EntityUid uid, WaveGameRuleComponent comp,
        GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, comp, gameRule, ref args);
        args.AddLine(Loc.GetString("final-stand-round-end",
            ("wave", comp.WavesCompleted),
            ("killed", comp.TotalEnemiesKilled)));
    }

    // prep phase, phase transitons

    private void StartPrepPhase(EntityUid uid, WaveGameRuleComponent comp)
    {
        comp.Phase = WavePhase.Prep;
        comp.PhaseEndTime = Timing.CurTime + comp.PrepDuration;
        Log.Info($"[WaveGameRule] Prep phase started. Wave {comp.WaveNumber} begins in {comp.PrepDuration.TotalSeconds}s.");
        RaiseNetworkEvent(new WaveCounterUpdateEvent(comp.WavesCompleted), Filter.Broadcast());
        Log.Info($"[WaveGameRule] WaveEndSound is {(comp.WaveEndSound == null ? "NULL" : comp.WaveEndSound.ToString())}");
        if (comp.WavesCompleted > 0 && comp.WaveEndSound != null)
            _audio.PlayGlobal(comp.WaveEndSound, Filter.Broadcast(), true);
    }

    private void StartCombatPhase(EntityUid uid, WaveGameRuleComponent comp)
    {
        comp.Phase = WavePhase.Combat;

        comp.SpawnerEntities.Clear();
        var sq = EntityQueryEnumerator<WaveEnemySpawnerComponent, TransformComponent>();
        while (sq.MoveNext(out var spawnerUid, out _, out _))
            comp.SpawnerEntities.Add(spawnerUid);

        if (comp.SpawnerEntities.Count == 0)
            Log.Warning($"[WaveGameRule] No WaveEnemySpawner entities found! Wave {comp.WaveNumber} will be empty.");

        comp.CCCEntity = EntityUid.Invalid;
        var cq = EntityQueryEnumerator<FinalStandCCCComponent>();
        if (cq.MoveNext(out var cccUid, out _))
            comp.CCCEntity = cccUid;

        if (!comp.CCCEntity.IsValid())
            Log.Warning("[WaveGameRule] No FinalStandCCC entity found — enemies will not beeline to objective.");

        comp.EnemyTotalThisWave = 5 * comp.WaveNumber;
        comp.EnemiesSpawnedThisWave = 0;
        comp.AliveEnemies.Clear();
        comp.PhaseEndTime = Timing.CurTime + comp.MaxCombatDuration;
        comp.NextSpawnTime = Timing.CurTime;

        var pool = GetDirectorPool(comp);
        Log.Info($"[WaveGameRule] Wave {comp.WaveNumber} started. Spawning {comp.EnemyTotalThisWave} enemies " +
                 $"at {comp.SpawnerEntities.Count} spawners. Director pool: {pool.Count} type(s).");

        RaiseNetworkEvent(new WaveCounterUpdateEvent(comp.WaveNumber), Filter.Broadcast());
        Log.Info($"[WaveGameRule] WaveStartSound is {(comp.WaveStartSound == null ? "NULL" : comp.WaveStartSound.ToString())}");
        if (comp.WaveStartSound != null)
            _audio.PlayGlobal(comp.WaveStartSound, Filter.Broadcast(), true);
    }

    private void EndCombatPhase(EntityUid uid, WaveGameRuleComponent comp)
    {
        Log.Info($"[WaveGameRule] Wave {comp.WaveNumber} complete. Moving to prep for wave {comp.WaveNumber + 1}.");
        comp.WavesCompleted++;
        comp.WaveNumber++;
        StartPrepPhase(uid, comp);
    }

    private void CheckWaveComplete(EntityUid uid, WaveGameRuleComponent comp)
    {
        var allSpawned = comp.EnemiesSpawnedThisWave >= comp.EnemyTotalThisWave;
        var allDead = comp.AliveEnemies.Count == 0;
        Log.Debug($"[WaveGameRule] CheckWaveComplete: allSpawned={allSpawned} ({comp.EnemiesSpawnedThisWave}/{comp.EnemyTotalThisWave}), allDead={allDead} ({comp.AliveEnemies.Count} alive)");
        if (allSpawned && allDead)
            EndCombatPhase(uid, comp);
    }

    // wave spawning

    private void SpawnNextBatch(EntityUid uid, WaveGameRuleComponent comp)
    {
        var pool = GetDirectorPool(comp);
        var remaining = comp.EnemyTotalThisWave - comp.EnemiesSpawnedThisWave;
        var toSpawn = Math.Min(remaining, comp.SpawnerEntities.Count);

        for (var i = 0; i < toSpawn; i++)
        {
            var spawnerUid = comp.SpawnerEntities[i];
            if (!TryComp<TransformComponent>(spawnerUid, out var xform))
                continue;

            var proto = RobustRandom.Pick(pool);
            var enemy = Spawn(proto, xform.Coordinates);
            EnsureComp<WaveSpawnedTagComponent>(enemy);
            if (TryComp<HTNComponent>(enemy, out var htn))
            {
                htn.Blackboard.SetValue("VisionRadius", 1000f);
                htn.Blackboard.SetValue("AggroVisionRadius", 1000f);
                htn.Blackboard.SetValue(NPCBlackboard.NavSmash, true);
                htn.Blackboard.SetValue(NPCBlackboard.NavPry, true);
                if (comp.CCCEntity.IsValid())
                    htn.Blackboard.SetValue(NPCBlackboard.CurrentOrderedTarget, comp.CCCEntity);
            }
            comp.AliveEnemies.Add(enemy);
            comp.EnemiesSpawnedThisWave++;
            Log.Info($"[WaveGameRule] Spawned {proto} ({comp.EnemiesSpawnedThisWave}/{comp.EnemyTotalThisWave}) " +
                     $"at spawner {spawnerUid}.");
        }

        comp.NextSpawnTime = Timing.CurTime + comp.SpawnInterval;

        // guard for the uh the no-spawner edge case where all enemies were already counted before this batch
        CheckWaveComplete(uid, comp);
    }

    private static List<EntProtoId> GetDirectorPool(WaveGameRuleComponent comp)
    {
        WaveEnemyConfig? match = null;
        foreach (var config in comp.EnemyConfigs)
        {
            if (config.FromWave <= comp.WaveNumber &&
                (config.ToWave == null || comp.WaveNumber <= config.ToWave))
            {
                match = config;
            }
        }
        return match?.EnemyPool ?? new List<EntProtoId> { "MobXeno" };
    }

    // event handlerss

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || args.OldMobState == MobState.Dead)
            return;

        var query = EntityQueryEnumerator<WaveGameRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;
            if (!comp.AliveEnemies.Remove(args.Target))
                continue;

            comp.TotalEnemiesKilled++;
            Log.Info($"[WaveGameRule] Enemy {args.Target} died. {comp.AliveEnemies.Count} alive, " +
                     $"{comp.EnemyTotalThisWave - comp.EnemiesSpawnedThisWave} not yet spawned (wave {comp.WaveNumber}).");
            CheckWaveComplete(uid, comp);
            break;
        }
    }

    private void OnWaveEnemyShutdown(EntityUid uid, WaveSpawnedTagComponent comp, ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<WaveGameRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var ruleUid, out var ruleComp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(ruleUid, gameRule))
                continue;
            if (!ruleComp.AliveEnemies.Remove(uid))
                continue;

            CheckWaveComplete(ruleUid, ruleComp);
            break;
        }
    }

// forces next wave
    public void ForceNextWave(IConsoleShell shell)
    {
        var query = EntityQueryEnumerator<WaveGameRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;
            if (comp.Phase != WavePhase.Prep)
                continue;

            StartCombatPhase(uid, comp);
            shell.WriteLine($"Wave {comp.WaveNumber} started");
            return;
        }

        shell.WriteError("Forcenextwave cannot work because the WaveGameRule is not active");
    }
}
