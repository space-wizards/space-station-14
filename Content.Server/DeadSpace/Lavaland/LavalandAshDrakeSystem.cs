using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.DeadSpace.Lavaland.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.Lavaland;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Lavaland;

public sealed class LavalandAshDrakeSystem : EntitySystem
{
    private static readonly Vector2i[] Cardinals =
    {
        new(0, 1),
        new(1, 0),
        new(0, -1),
        new(-1, 0),
    };

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private readonly List<EntityUid> _participants = new();
    private readonly HashSet<Vector2i> _detonatingTiles = new();
    private readonly Dictionary<Vector2i, DamageSpecifier> _detonatingDamage = new();
    private readonly HashSet<Vector2i> _detonatingIgniteTiles = new();
    private readonly HashSet<EntityUid> _fireTileEntities = new();
    private readonly HashSet<EntityUid> _tileEntities = new();
    private readonly HashSet<EntityUid> _damagedEntities = new();
    private readonly HashSet<Vector2i> _cageFireTiles = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LavalandAshDrakeComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<LavalandAshDrakeComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<LavalandAshDrakeComponent, GettingAttackedAttemptEvent>(OnGettingAttackedAttempt);
        SubscribeLocalEvent<LavalandAshDrakeComponent, LavalandBossFightStartedEvent>(OnBossFightStarted);
        SubscribeLocalEvent<LavalandAshDrakeComponent, LavalandBossResetEvent>(OnBossReset);
        SubscribeLocalEvent<LavalandAshDrakeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        ProcessResidualFires(now);

        var query = EntityQueryEnumerator<LavalandAshDrakeComponent, LavalandBossComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var drake, out var boss, out var xform))
        {
            if (boss.Arena is not { Valid: true } arenaUid ||
                !TryComp<LavalandBossArenaComponent>(arenaUid, out var arena) ||
                arena.Ended ||
                !arena.FightStarted ||
                xform.GridUid != arena.Grid ||
                !TryComp<MapGridComponent>(arena.Grid, out var grid) ||
                IsDead(uid))
            {
                ClearRuntimeState(uid, drake, false);
                continue;
            }

            ProcessPendingTiles(uid, drake, arena.Grid, grid, now);

            var participantCount = CollectParticipants(arena);
            if (participantCount == 0)
            {
                ClearRuntimeState(uid, drake, true);
                drake.BusyUntil = TimeSpan.Zero;
                continue;
            }

            ProcessHealthPhases(uid, drake, boss, arena, arena.Grid, grid, now);
            ProcessWhelpAttacks(drake, grid, now);

            if (drake.CageActive)
            {
                ProcessCageAttack(uid, drake, arena, arena.Grid, grid, now);
                continue;
            }

            if (ProcessSwoop(uid, drake, arena, arena.Grid, grid, now) ||
                ProcessQueuedSwoop(uid, drake, arena, arena.Grid, grid, now))
            {
                continue;
            }

            if (drake.BusyUntil > now ||
                drake.NextAttack > now)
            {
                continue;
            }

            var target = PickTarget(drake, uid, arena.Grid, grid, now);
            if (target == null)
                continue;

            RunAttack(uid, drake, arena, arena.Grid, grid, target.Value, now);
        }
    }

    private void ProcessResidualFires(TimeSpan now)
    {
        var query = EntityQueryEnumerator<LavalandAshDrakeFireComponent, TransformComponent>();
        while (query.MoveNext(out var fireUid, out var fire, out var xform))
        {
            if (fire.SpawnedAt == TimeSpan.Zero)
                fire.SpawnedAt = now;

            if (xform.GridUid is not { Valid: true } gridUid ||
                !TryComp<MapGridComponent>(gridUid, out var grid))
            {
                continue;
            }

            var fireTile = _map.LocalToTile(gridUid, grid, xform.Coordinates);
            _fireTileEntities.Clear();
            _lookup.GetLocalEntitiesIntersecting(
                gridUid,
                fireTile,
                _fireTileEntities,
                0f,
                LookupFlags.Dynamic | LookupFlags.Sundries,
                grid);

            foreach (var uid in _fireTileEntities)
            {
                if (uid == fireUid ||
                    HasComp<LavalandBossComponent>(uid) ||
                    !TryComp(uid, out DamageableComponent? damageable) ||
                    !TryComp(uid, out MobStateComponent? mobState) ||
                    mobState.CurrentState == MobState.Dead ||
                    !TryComp(uid, out TransformComponent? targetXform) ||
                    targetXform.GridUid != gridUid)
                {
                    continue;
                }

                var targetTile = _map.LocalToTile(gridUid, grid, targetXform.Coordinates);
                if (targetTile != fireTile)
                    continue;

                var firstDamageAt = fire.SpawnedAt + fire.InitialDelay;
                if (!fire.NextDamageByEntity.TryGetValue(uid, out var nextDamage))
                    nextDamage = now < firstDamageAt ? firstDamageAt : now;

                if (nextDamage > now)
                {
                    fire.NextDamageByEntity[uid] = nextDamage;
                    continue;
                }

                _damageable.TryChangeDamage((uid, damageable), fire.Damage, origin: fireUid);
                IgniteEntity(uid, fire.FireStacks, fireUid);
                fire.NextDamageByEntity[uid] = now + fire.DamageInterval;
            }

            _fireTileEntities.Clear();
        }
    }

    private void OnBeforeDamage(Entity<LavalandAshDrakeComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (IsSwoopUntargetable(ent.Comp) || ent.Comp.CageActive)
            args.Cancelled = true;
    }

    private void OnAttackAttempt(EntityUid uid, LavalandAshDrakeComponent component, AttackAttemptEvent args)
    {
        if (IsSwoopUntargetable(component))
            args.Cancel();
    }

    private void OnGettingAttackedAttempt(EntityUid uid, LavalandAshDrakeComponent component, ref GettingAttackedAttemptEvent args)
    {
        if (IsSwoopUntargetable(component))
            args.Cancelled = true;
    }

    private static bool IsSwoopUntargetable(LavalandAshDrakeComponent component)
    {
        return component.Swooping ||
               component.SwoopInvulnerable ||
               component.SwoopImpactAt != TimeSpan.Zero;
    }

    private void OnBossReset(EntityUid uid, LavalandAshDrakeComponent component, LavalandBossResetEvent args)
    {
        PrepareFight(uid, component);
    }

    private void OnBossFightStarted(EntityUid uid, LavalandAshDrakeComponent component, LavalandBossFightStartedEvent args)
    {
        PrepareFight(uid, component);
    }

    private void PrepareFight(EntityUid uid, LavalandAshDrakeComponent component)
    {
        ClearRuntimeState(uid, component, true);
        var now = _timing.CurTime;
        component.NextAttack = now + TimeSpan.FromSeconds(1);
        component.LastPressureAt = now;
        component.LastAttackKind = string.Empty;
    }

    private void OnRefreshMovementSpeed(EntityUid uid, LavalandAshDrakeComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.Swooping || component.CageActive)
            args.ModifySpeed(0f, 0f);
    }

    private void RunAttack(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        EntityUid target,
        TimeSpan now)
    {
        var rage = CalculateRage(boss);
        var belowHalf = IsBelowHalfHealth(boss);
        var targetTile = GetEntityTile(target, gridUid, grid);
        var bossTile = GetEntityTile(boss, gridUid, grid);
        if (targetTile == null || bossTile == null)
        {
            drake.NextAttack = now + TimeSpan.FromSeconds(0.5);
            return;
        }

        var distance = ChebyshevDistance(bossTile.Value, targetTile.Value);
        var targetNearEdge = IsNearInnerArenaEdge(arena, targetTile.Value, 5);
        var targetFar = distance >= Math.Max(10, arena.Width / 4);
        var forcePressure = NeedsPressure(drake, now);

        if (belowHalf &&
            now >= drake.NextWhelpWave &&
            _random.Prob(drake.WhelpWaveChance))
        {
            SpawnWhelpWave(drake, arena, gridUid, grid, now);
            drake.NextWhelpWave = now + drake.WhelpWaveCooldown;
            MarkPressure(drake, now, "whelp-wave", target);
            drake.NextAttack = now + GetScaledCooldown(drake.RangedCooldown, rage);
            return;
        }
        if (belowHalf &&
            !forcePressure &&
            !targetFar &&
            !targetNearEdge &&
            _random.Prob(drake.CageAttackChance))
        {
            StartCageAttack(boss, drake, arena, gridUid, grid, target, now);
            MarkPressure(drake, now, "cage", target);
            drake.NextAttack = now + GetScaledCooldown(drake.RangedCooldown, rage);
            return;
        }

        if (forcePressure)
        {
            if (targetFar || belowHalf || _random.Prob(0.55f))
            {
                StartSwoop(boss, drake, arena, gridUid, grid, target, belowHalf ? drake.TripleSwoopSteps : drake.SwoopSteps, true, now, belowHalf ? 1 : 0, true);
                MarkPressure(drake, now, "forced-swoop", target);
            }
            else
            {
                QueueFireRain(drake, arena, gridUid, grid, target, now, true);
                MarkPressure(drake, now, "forced-fire-rain", target);
            }

            drake.NextAttack = now + GetScaledCooldown(drake.RangedCooldown, rage);
            return;
        }

        if (targetFar || targetNearEdge)
        {
            if (_random.Prob(belowHalf ? 0.55f : 0.35f))
            {
                StartSwoop(boss, drake, arena, gridUid, grid, target, belowHalf ? drake.TripleSwoopSteps : drake.SwoopSteps, true, now, belowHalf ? 1 : 0, true);
                MarkPressure(drake, now, targetNearEdge ? "edge-swoop" : "far-swoop", target);
            }
            else
            {
                QueueFireRain(drake, arena, gridUid, grid, target, now, true);
                MarkPressure(drake, now, targetNearEdge ? "edge-fire-rain" : "far-fire-rain", target);
            }

            drake.NextAttack = now + GetScaledCooldown(drake.RangedCooldown, rage);
            return;
        }

        if (_random.Prob((22f + rage * 1.1f) / 100f))
        {
            if (belowHalf)
                StartSwoop(boss, drake, arena, gridUid, grid, target, drake.SwoopSteps, true, now, 0, true);
            else
                QueueFireRain(drake, arena, gridUid, grid, target, now, false);

            MarkPressure(drake, now, belowHalf ? "swoop-fire-rain" : "fire-rain", target);
            drake.NextAttack = now + GetScaledCooldown(drake.RangedCooldown, rage);
            return;
        }

        if (_random.Prob((18f + rage) / 100f))
        {
            if (belowHalf)
                StartSwoop(boss, drake, arena, gridUid, grid, target, drake.TripleSwoopSteps, false, now, 2, true);
            else
                StartSwoop(boss, drake, arena, gridUid, grid, target, drake.SwoopSteps, false, now, 0, true);

            MarkPressure(drake, now, belowHalf ? "triple-swoop" : "swoop", target);
            drake.NextAttack = now + GetScaledCooldown(drake.RangedCooldown, rage);
            return;
        }

        QueueFireWalls(boss, drake, arena, gridUid, grid, now);
        MarkPressure(drake, now, "fire-walls", target);
        drake.NextAttack = now + GetScaledCooldown(drake.RangedCooldown, rage);
    }

    private void StartCageAttack(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        EntityUid target,
        TimeSpan now)
    {
        var targetTile = GetEntityTile(target, gridUid, grid);
        if (targetTile == null)
            return;

        var cageCenter = GetCageCenter(arena, targetTile.Value, drake.CageHalfSize);

        drake.CageCenter = cageCenter;
        drake.CageActive = true;
        drake.CagePhase = 0;
        drake.CageEndAt = now + drake.CageDuration;
        drake.NextCageTargetAt = now + TimeSpan.FromSeconds(0.5);
        drake.NextCageFillAt = TimeSpan.Zero;
        drake.NextCageDamageTick = now + drake.CageDamageInterval;
        drake.CurrentCageTargetTile = null;
        drake.CageTargetEntity = null;
        drake.CageSafeTile = null;

        SpawnCageBorder(drake, gridUid, grid, cageCenter);

        var (minX, maxX, minY, maxY) = GetInnerBounds(arena);
        var centerTile = new Vector2i((minX + maxX) / 2, (minY + maxY) / 2);
        _transform.SetCoordinates(boss, _map.GridTileToLocal(gridUid, grid, centerTile));

        _audio.PlayPvs(drake.FireSound, boss, AudioParams.Default.WithVolume(2f));

        SetVisual(boss, LavalandAshDrakeVisualState.Shadow);
        _movement.RefreshMovementSpeedModifiers(boss);

        drake.BusyUntil = drake.CageEndAt + TimeSpan.FromSeconds(3);
    }

    private void ProcessCageAttack(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        if (now >= drake.CageEndAt)
        {
            EndCageAttack(boss, drake, arena, gridUid, grid, now);
            return;
        }

        if (now >= drake.NextCageDamageTick)
        {
            ApplyCageDamage(boss, drake, gridUid, grid);
            drake.NextCageDamageTick = now + drake.CageDamageInterval;
        }

        if (drake.CurrentCageTargetTile.HasValue &&
            drake.NextCageFillAt != TimeSpan.Zero &&
            now >= drake.NextCageFillAt)
        {
            FillCagePhase(boss, drake, gridUid, grid, now);
            return;
        }

        if (!drake.CurrentCageTargetTile.HasValue && now >= drake.NextCageTargetAt)
        {
            SpawnCageTarget(drake, gridUid, grid);
            if (drake.CurrentCageTargetTile.HasValue)
                drake.NextCageFillAt = now + drake.CageTargetInterval;
        }
    }

    private void FillCagePhase(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        var safeTile = drake.CurrentCageTargetTile!.Value;

        if (drake.CageTargetEntity.HasValue && Exists(drake.CageTargetEntity.Value))
            QueueDel(drake.CageTargetEntity.Value);
        drake.CageTargetEntity = null;

        FillCageInterior(drake, gridUid, grid, safeTile);

        drake.CageSafeTile = safeTile;
        drake.CurrentCageTargetTile = null;
        drake.NextCageFillAt = TimeSpan.Zero;
        drake.CagePhase++;

        _audio.PlayPvs(
            drake.ImpactSound,
            _map.GridTileToLocal(gridUid, grid, safeTile),
            AudioParams.Default.WithVolume(-3f));

        if (drake.CagePhase >= drake.CagePhaseCount)
        {
            drake.CageEndAt = now + TimeSpan.FromSeconds(0.5);
            drake.NextCageTargetAt = TimeSpan.MaxValue;
            return;
        }

        drake.NextCageTargetAt = now + drake.CageTargetInterval;
    }

    private void EndCageAttack(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        foreach (var entity in drake.CageBorderEntities)
        {
            if (Exists(entity))
                QueueDel(entity);
        }
        drake.CageBorderEntities.Clear();

        foreach (var entity in drake.CageInteriorEntities)
        {
            if (Exists(entity))
                QueueDel(entity);
        }
        drake.CageInteriorEntities.Clear();

        if (drake.CageTargetEntity.HasValue && Exists(drake.CageTargetEntity.Value))
            QueueDel(drake.CageTargetEntity.Value);
        drake.CageTargetEntity = null;

        drake.CageActive = false;
        drake.CagePhase = 0;
        drake.CurrentCageTargetTile = null;
        drake.CageSafeTile = null;
        drake.NextCageFillAt = TimeSpan.Zero;

        if (!Exists(boss))
            return;

        SetVisual(boss, LavalandAshDrakeVisualState.Dragon);
        _movement.RefreshMovementSpeedModifiers(boss);

        var (minX, maxX, minY, maxY) = GetInnerBounds(arena);
        var centerTile = new Vector2i((minX + maxX) / 2, (minY + maxY) / 2);
        _transform.SetCoordinates(boss, _map.GridTileToLocal(gridUid, grid, centerTile));

        _audio.PlayPvs(drake.ImpactSound, boss, AudioParams.Default.WithVolume(0f));

        drake.BusyUntil = now + TimeSpan.FromSeconds(1.5);
        drake.NextAttack = now + TimeSpan.FromSeconds(3.0);
    }

    private void SpawnCageBorder(
        LavalandAshDrakeComponent drake,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i center)
    {
        var half = drake.CageHalfSize;

        for (var x = -half; x <= half; x++)
        {
            for (var y = -half; y <= half; y++)
            {
                if (Math.Abs(x) != half && Math.Abs(y) != half)
                    continue;

                var tile = center + new Vector2i(x, y);
                var uid = Spawn(drake.CageBorderFirePrototype, _map.GridTileToLocal(gridUid, grid, tile));
                if (TryComp(uid, out TransformComponent? xform) && !xform.Anchored)
                    _transform.AnchorEntity((uid, xform), (gridUid, grid), tile);
                drake.CageBorderEntities.Add(uid);
            }
        }
    }

    private void SpawnCageTarget(
        LavalandAshDrakeComponent drake,
        EntityUid gridUid,
        MapGridComponent grid)
    {
        var innerHalf = drake.CageHalfSize - 1;
        var center = drake.CageCenter;

        var candidates = new List<Vector2i>();
        for (var x = -innerHalf; x <= innerHalf; x++)
        {
            for (var y = -innerHalf; y <= innerHalf; y++)
            {
                var tile = center + new Vector2i(x, y);

                if (drake.CageSafeTile.HasValue && tile == drake.CageSafeTile.Value)
                    continue;

                candidates.Add(tile);
            }
        }

        if (candidates.Count == 0)
            return;

        var targetTile = _random.Pick(candidates);
        drake.CurrentCageTargetTile = targetTile;

        var uid = Spawn(drake.CageTargetPrototype, _map.GridTileToLocal(gridUid, grid, targetTile));
        if (TryComp(uid, out TransformComponent? xform) && !xform.Anchored)
            _transform.AnchorEntity((uid, xform), (gridUid, grid), targetTile);
        drake.CageTargetEntity = uid;
    }

    private void FillCageInterior(
        LavalandAshDrakeComponent drake,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i safeTile)
    {
        foreach (var entity in drake.CageInteriorEntities)
        {
            if (Exists(entity))
                QueueDel(entity);
        }
        drake.CageInteriorEntities.Clear();

        var innerHalf = drake.CageHalfSize - 1;
        var center = drake.CageCenter;

        for (var x = -innerHalf; x <= innerHalf; x++)
        {
            for (var y = -innerHalf; y <= innerHalf; y++)
            {
                var tile = center + new Vector2i(x, y);

                if (tile == safeTile)
                    continue;

                var uid = Spawn(drake.CageFirePrototype, _map.GridTileToLocal(gridUid, grid, tile));
                if (TryComp(uid, out TransformComponent? xform) && !xform.Anchored)
                    _transform.AnchorEntity((uid, xform), (gridUid, grid), tile);
                drake.CageInteriorEntities.Add(uid);
            }
        }
    }

    private void ApplyCageDamage(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        EntityUid gridUid,
        MapGridComponent grid)
    {
        _cageFireTiles.Clear();
        foreach (var entity in drake.CageInteriorEntities)
        {
            if (!Exists(entity) || !TryComp(entity, out TransformComponent? fxform))
                continue;

            _cageFireTiles.Add(_map.LocalToTile(gridUid, grid, fxform.Coordinates));
        }

        var half = drake.CageHalfSize;
        var center = drake.CageCenter;
        _damagedEntities.Clear();

        foreach (var fireTile in _cageFireTiles)
        {
            _tileEntities.Clear();
            _lookup.GetLocalEntitiesIntersecting(
                gridUid,
                fireTile,
                _tileEntities,
                flags: LookupFlags.Dynamic | LookupFlags.Sundries,
                gridComp: grid);

            foreach (var uid in _tileEntities)
            {
                if (uid == boss ||
                    !_damagedEntities.Add(uid) ||
                    !TryComp(uid, out DamageableComponent? damageable) ||
                    !TryComp(uid, out MobStateComponent? mobState) ||
                    mobState.CurrentState == MobState.Dead ||
                    !TryComp(uid, out TransformComponent? xform) ||
                    xform.GridUid != gridUid)
                {
                    continue;
                }

                var tile = _map.LocalToTile(gridUid, grid, xform.Coordinates);
                var dx = Math.Abs(tile.X - center.X);
                var dy = Math.Abs(tile.Y - center.Y);

                if (tile != fireTile ||
                    dx > half ||
                    dy > half)
                {
                    continue;
                }

                _damageable.TryChangeDamage((uid, damageable), drake.CageInteriorFireDamage, origin: boss);
                IgniteEntity(uid, drake, boss);
                _audio.PlayPvs(drake.HitSound, uid, AudioParams.Default.WithVolume(-6f));
            }
        }

        _tileEntities.Clear();
        _damagedEntities.Clear();
        _cageFireTiles.Clear();
    }

    private static Vector2i GetCageCenter(LavalandBossArenaComponent arena, Vector2i target, int halfSize)
    {
        var (minX, maxX, minY, maxY) = GetInnerBounds(arena);
        return new Vector2i(
            Math.Clamp(target.X, minX + halfSize, maxX - halfSize),
            Math.Clamp(target.Y, minY + halfSize, maxY - halfSize));
    }

    private void QueueFireWalls(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        var bossTile = GetEntityTile(boss, gridUid, grid);
        if (bossTile == null)
            return;

        _audio.PlayPvs(drake.FireSound, boss, AudioParams.Default.WithVolume(-1f));

        var maxStep = 0;
        var range = Math.Max(0, drake.FireWallRange);
        var stepDelay = Math.Max(0, drake.FireWallStepDelay.TotalSeconds);
        foreach (var direction in Cardinals)
        {
            for (var step = 1; step <= range; step++)
            {
                var tile = bossTile.Value + direction * step;
                if (!IsInsideInnerArena(arena, tile))
                    break;

                QueuePendingTile(
                    drake,
                    gridUid,
                    grid,
                    tile,
                    now + TimeSpan.FromSeconds(stepDelay * step),
                    drake.FireWallDamage,
                    drake.FirePrototype,
                    true,
                    false);

                maxStep = Math.Max(maxStep, step);
            }
        }

        drake.BusyUntil = now + TimeSpan.FromSeconds(stepDelay * maxStep + 0.4);
    }

    private void QueueFireRain(
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        EntityUid target,
        TimeSpan now,
        bool focused)
    {
        var targetTile = GetEntityTile(target, gridUid, grid);
        if (targetTile == null)
            return;

        var radius = Math.Max(0, drake.FireRainRadius);
        var chance = Math.Clamp(drake.FireRainTileChance, 0f, 1f);
        var maxTiles = Math.Max(0, drake.FireRainMaxTiles);
        var priority = new List<Vector2i>();
        var candidates = new List<Vector2i>();

        AddFireRainCandidate(priority, arena, targetTile.Value);
        if (focused)
        {
            foreach (var direction in Cardinals)
            {
                AddFireRainCandidate(priority, arena, targetTile.Value + direction);
                AddFireRainCandidate(priority, arena, targetTile.Value + direction * 2);
            }
        }

        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var tile = targetTile.Value + new Vector2i(x, y);
                if (!IsInsideInnerArena(arena, tile) ||
                    ChebyshevDistance(targetTile.Value, tile) > radius ||
                    !_random.Prob(chance) ||
                    priority.Contains(tile) ||
                    candidates.Contains(tile))
                {
                    continue;
                }

                candidates.Add(tile);
            }
        }

        while (priority.Count + candidates.Count > maxTiles && candidates.Count > 0)
            candidates.RemoveAt(_random.Next(candidates.Count));

        foreach (var tile in priority)
            QueueFireRainTile(drake, gridUid, grid, tile, now);

        foreach (var tile in candidates)
            QueueFireRainTile(drake, gridUid, grid, tile, now);

        if (priority.Count > 0)
            _audio.PlayPvs(drake.FireRainSound, _map.GridTileToLocal(gridUid, grid, priority[0]), AudioParams.Default.WithVolume(-1f));
        else if (candidates.Count > 0)
            _audio.PlayPvs(drake.FireRainSound, _map.GridTileToLocal(gridUid, grid, candidates[0]), AudioParams.Default.WithVolume(-1f));

        drake.BusyUntil = now + drake.FireRainDelay + TimeSpan.FromSeconds(0.4);

        static void AddFireRainCandidate(List<Vector2i> output, LavalandBossArenaComponent arena, Vector2i tile)
        {
            if (IsInsideInnerArena(arena, tile) && !output.Contains(tile))
                output.Add(tile);
        }
    }

    private void QueueFireRainTile(
        LavalandAshDrakeComponent drake,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i tile,
        TimeSpan now)
    {
        if (!QueuePendingTile(
            drake,
            gridUid,
            grid,
            tile,
            now + drake.FireRainDelay,
            drake.FireRainDamage,
            drake.FirePrototype,
            true,
            true))
        {
            return;
        }

        SpawnAnchored(drake.FireRainTargetPrototype, gridUid, grid, tile);
        SpawnAnchored(drake.FireRainFireballPrototype, gridUid, grid, tile);
    }

    private bool QueuePendingTile(
        LavalandAshDrakeComponent drake,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i tile,
        TimeSpan detonateAt,
        DamageSpecifier damage,
        string effectPrototype,
        bool ignite,
        bool playImpactSound)
    {
        if (drake.PendingTiles.Count >= Math.Max(0, drake.MaxPendingTiles))
            return false;

        foreach (var pending in drake.PendingTiles)
        {
            if (pending.Grid == gridUid &&
                pending.Tile == tile &&
                Math.Abs((pending.DetonateAt - detonateAt).TotalSeconds) <= 0.2)
            {
                return false;
            }
        }

        drake.PendingTiles.Add(new LavalandAshDrakePendingTile
        {
            Grid = gridUid,
            Tile = tile,
            DetonateAt = detonateAt,
            Damage = damage,
            Ignite = ignite,
            PlayImpactSound = playImpactSound,
            EffectPrototype = effectPrototype,
        });
        return true;
    }

    private void StartSwoop(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        EntityUid target,
        int steps,
        bool dropsFireRain,
        TimeSpan now,
        int extraSwoops,
        bool replacePending)
    {
        var targetTile = GetEntityTile(target, gridUid, grid);
        if (targetTile == null)
            return;

        drake.Swooping = true;
        drake.SwoopInvulnerable = true;
        drake.SwoopTarget = target;
        drake.SwoopRemainingSteps = Math.Max(1, steps);
        drake.SwoopDropsFireRain = dropsFireRain;
        drake.SwoopFireRainTilesQueued = 0;
        drake.NextSwoopStep = now + drake.SwoopWindup;
        drake.SwoopImpactAt = TimeSpan.Zero;

        if (replacePending)
        {
            drake.PendingSwoops = Math.Max(0, extraSwoops);
            drake.PendingSwoopSteps = Math.Max(1, steps);
            drake.NextQueuedSwoop = TimeSpan.Zero;
        }

        SetVisual(boss, LavalandAshDrakeVisualState.Shadow);
        _movement.RefreshMovementSpeedModifiers(boss);

        drake.BusyUntil = now + drake.SwoopWindup + TimeSpan.FromSeconds(drake.SwoopStepDelay.TotalSeconds * steps) + drake.SwoopRecover;
    }

    private bool ProcessSwoop(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        if (!drake.Swooping)
            return false;

        if (drake.SwoopImpactAt != TimeSpan.Zero)
        {
            if (now >= drake.SwoopImpactAt)
                FinishSwoop(boss, drake, arena, gridUid, grid, now);

            return true;
        }

        if (now < drake.NextSwoopStep)
            return true;

        drake.SwoopInvulnerable = true;

        var currentTile = GetEntityTile(boss, gridUid, grid);
        if (currentTile == null)
        {
            QueueSwoopImpact(boss, drake, gridUid, grid, now);
            return true;
        }

        var targetTile = GetSwoopTargetTile(drake, arena, gridUid, grid, now) ?? currentTile.Value;
        if (currentTile.Value == targetTile || drake.SwoopRemainingSteps <= 0)
        {
            QueueSwoopImpact(boss, drake, gridUid, grid, now);
            return true;
        }

        if (drake.SwoopDropsFireRain &&
            drake.SwoopFireRainTilesQueued < drake.SwoopFireRainMaxTiles)
        {
            QueueFireRainTile(drake, gridUid, grid, currentTile.Value, now);
            drake.SwoopFireRainTilesQueued++;
        }

        var nextTile = StepTowards(currentTile.Value, targetTile);
        if (!IsInsideInnerArena(arena, nextTile))
            nextTile = ClampToInnerArena(arena, nextTile);

        _transform.SetCoordinates(boss, _map.GridTileToLocal(gridUid, grid, nextTile));
        SetVisual(boss, LavalandAshDrakeVisualState.Shadow);

        drake.SwoopRemainingSteps--;
        drake.NextSwoopStep = now + drake.SwoopStepDelay;

        if (nextTile == targetTile || drake.SwoopRemainingSteps <= 0)
            QueueSwoopImpact(boss, drake, gridUid, grid, drake.NextSwoopStep);

        return true;
    }

    private bool ProcessQueuedSwoop(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        if (drake.PendingSwoops <= 0)
            return false;

        if (now < drake.NextQueuedSwoop)
            return true;

        var target = PickTarget(drake, boss, gridUid, grid, now);
        if (target == null)
        {
            drake.PendingSwoops = 0;
            return false;
        }

        drake.PendingSwoops--;
        StartSwoop(boss, drake, arena, gridUid, grid, target.Value, drake.PendingSwoopSteps, false, now, 0, false);
        return true;
    }

    private Vector2i? GetSwoopTargetTile(
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        if (drake.SwoopTarget is not { Valid: true } target ||
            !Exists(target) ||
            IsDead(target))
        {
            var fallback = PickTarget(drake, EntityUid.Invalid, gridUid, grid, now);
            if (fallback == null)
                return null;

            drake.SwoopTarget = fallback;
            target = fallback.Value;
        }

        var tile = GetEntityTile(target, gridUid, grid);
        if (tile == null)
            return null;

        return ClampToInnerArena(arena, tile.Value);
    }

    private void QueueSwoopImpact(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        if (drake.SwoopImpactAt != TimeSpan.Zero)
            return;

        var tile = GetEntityTile(boss, gridUid, grid) ?? Vector2i.Zero;
        SpawnAnchored(drake.LandingPrototype, gridUid, grid, tile);
        SetVisual(boss, LavalandAshDrakeVisualState.Swoop);

        drake.SwoopImpactAt = now + drake.SwoopRecover;
        drake.BusyUntil = drake.SwoopImpactAt + TimeSpan.FromSeconds(0.25);
    }

    private void FinishSwoop(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        var tile = GetEntityTile(boss, gridUid, grid) ?? Vector2i.Zero;
        _audio.PlayPvs(drake.ImpactSound, _map.GridTileToLocal(gridUid, grid, tile), AudioParams.Default.WithVolume(1f));

        DamageArea(boss, drake, gridUid, grid, tile, 1, drake.SwoopDamage, true, true);

        drake.Swooping = false;
        drake.SwoopInvulnerable = false;
        drake.SwoopTarget = null;
        drake.SwoopImpactAt = TimeSpan.Zero;
        drake.SwoopDropsFireRain = false;
        drake.SwoopFireRainTilesQueued = 0;

        SetVisual(boss, LavalandAshDrakeVisualState.Dragon);
        _movement.RefreshMovementSpeedModifiers(boss);

        if (drake.PendingSwoops > 0)
        {
            drake.NextQueuedSwoop = now + drake.ChainedSwoopDelay;
            drake.BusyUntil = drake.NextQueuedSwoop + drake.SwoopWindup;
        }
        else
        {
            drake.BusyUntil = now + TimeSpan.FromSeconds(0.3);
        }
    }

    private void ProcessPendingTiles(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        _detonatingTiles.Clear();
        _detonatingDamage.Clear();
        _detonatingIgniteTiles.Clear();

        Vector2i? impactSoundTile = null;
        for (var i = drake.PendingTiles.Count - 1; i >= 0; i--)
        {
            var pending = drake.PendingTiles[i];
            if (pending.DetonateAt > now)
                continue;

            if (pending.Grid == gridUid)
            {
                SpawnAnchored(pending.EffectPrototype, gridUid, grid, pending.Tile);
                _detonatingTiles.Add(pending.Tile);
                _detonatingDamage[pending.Tile] = pending.Damage;

                if (pending.Ignite)
                    _detonatingIgniteTiles.Add(pending.Tile);

                if (pending.PlayImpactSound)
                    impactSoundTile ??= pending.Tile;
            }

            drake.PendingTiles.RemoveAt(i);
        }

        if (_detonatingTiles.Count == 0)
            return;

        if (impactSoundTile != null)
            _audio.PlayPvs(drake.ImpactSound, _map.GridTileToLocal(gridUid, grid, impactSoundTile.Value), AudioParams.Default.WithVolume(-4f));

        DamageDetonatingTiles(boss, drake, gridUid, grid);
    }

    private void DamageDetonatingTiles(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        EntityUid gridUid,
        MapGridComponent grid)
    {
        _damagedEntities.Clear();

        foreach (var tile in _detonatingTiles)
        {
            if (!_detonatingDamage.TryGetValue(tile, out var damage))
                continue;

            _tileEntities.Clear();
            _lookup.GetLocalEntitiesIntersecting(
                gridUid,
                tile,
                _tileEntities,
                flags: LookupFlags.Dynamic | LookupFlags.Sundries,
                gridComp: grid);

            foreach (var uid in _tileEntities)
            {
                if (uid == boss ||
                    !_damagedEntities.Add(uid) ||
                    !TryComp(uid, out DamageableComponent? damageable) ||
                    !TryComp(uid, out MobStateComponent? mobState) ||
                    mobState.CurrentState == MobState.Dead ||
                    !TryComp(uid, out TransformComponent? xform) ||
                    xform.GridUid != gridUid ||
                    _map.LocalToTile(gridUid, grid, xform.Coordinates) != tile)
                {
                    continue;
                }

                IgniteEntity(uid, drake, boss);
                _damageable.TryChangeDamage((uid, damageable), damage, origin: boss);

                if (_detonatingIgniteTiles.Contains(tile))
                    IgniteEntity(uid, drake, boss);

                _audio.PlayPvs(drake.HitSound, uid, AudioParams.Default.WithVolume(-6f));
            }
        }

        _tileEntities.Clear();
        _damagedEntities.Clear();
    }

    private void DamageArea(
        EntityUid boss,
        LavalandAshDrakeComponent drake,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i center,
        int radius,
        DamageSpecifier damage,
        bool ignite,
        bool throwTargets)
    {
        _damagedEntities.Clear();

        for (var x = center.X - radius; x <= center.X + radius; x++)
        {
            for (var y = center.Y - radius; y <= center.Y + radius; y++)
            {
                var tile = new Vector2i(x, y);
                if (ChebyshevDistance(center, tile) > radius)
                    continue;

                _tileEntities.Clear();
                _lookup.GetLocalEntitiesIntersecting(
                    gridUid,
                    tile,
                    _tileEntities,
                    flags: LookupFlags.Dynamic | LookupFlags.Sundries,
                    gridComp: grid);

                foreach (var uid in _tileEntities)
                {
                    if (uid == boss ||
                        !_damagedEntities.Add(uid) ||
                        !TryComp(uid, out DamageableComponent? damageable) ||
                        !TryComp(uid, out MobStateComponent? mobState) ||
                        mobState.CurrentState == MobState.Dead ||
                        !TryComp(uid, out TransformComponent? xform) ||
                        xform.GridUid != gridUid ||
                        _map.LocalToTile(gridUid, grid, xform.Coordinates) != tile)
                    {
                        continue;
                    }

                    _damageable.TryChangeDamage((uid, damageable), damage, origin: boss);

                    if (ignite)
                        IgniteEntity(uid, drake, boss);

                    if (throwTargets)
                    {
                        var direction = new Vector2(tile.X - center.X, tile.Y - center.Y);
                        if (direction.LengthSquared() < 0.01f)
                            direction = _random.NextVector2();

                        _throwing.TryThrow(uid, direction.Normalized() * 2.5f, drake.SwoopThrowSpeed, boss, playSound: false, doSpin: false);
                    }

                    _audio.PlayPvs(drake.HitSound, uid, AudioParams.Default.WithVolume(-4f));
                }
            }
        }

        _tileEntities.Clear();
        _damagedEntities.Clear();
    }

    private void IgniteEntity(EntityUid uid, LavalandAshDrakeComponent drake, EntityUid source)
    {
        IgniteEntity(uid, drake.FireStacks, source);
    }

    private void IgniteEntity(EntityUid uid, float fireStacks, EntityUid source)
    {
        if (fireStacks <= 0f ||
            !TryComp(uid, out FlammableComponent? flammable))
        {
            return;
        }

        _flammable.AdjustFireStacks(uid, fireStacks, flammable, true);
        _flammable.Ignite(uid, source, flammable);
    }

    private int CollectParticipants(LavalandBossArenaComponent arena)
    {
        _participants.Clear();

        foreach (var userId in arena.Participants)
        {
            if (!_players.TryGetSessionById(userId, out var session) ||
                session.AttachedEntity is not { Valid: true } attached ||
                !Exists(attached) ||
                IsDead(attached) ||
                !TryComp(attached, out TransformComponent? xform) ||
                xform.GridUid != arena.Grid)
            {
                continue;
            }

            _participants.Add(attached);
        }

        return _participants.Count;
    }

    private EntityUid? PickTarget(
        LavalandAshDrakeComponent drake,
        EntityUid boss,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        if (_participants.Count == 0)
            return null;

        PruneTargetMemory(drake);

        if (_participants.Count == 1)
        {
            SetPrimaryTarget(drake, _participants[0], now);
            return _participants[0];
        }

        if (drake.CurrentPrimaryTarget is { Valid: true } current &&
            _participants.Contains(current) &&
            now - drake.LastTargetSwitchAt < drake.TargetSwitchCooldown)
        {
            return current;
        }

        var bossTile = boss.Valid ? GetEntityTile(boss, gridUid, grid) : null;
        EntityUid? best = null;
        var bestScore = float.MinValue;
        foreach (var participant in _participants)
        {
            var tile = GetEntityTile(participant, gridUid, grid);
            if (tile == null)
                continue;

            var score = ScoreTarget(drake, participant, bossTile, tile.Value, now, true);
            if (score <= bestScore)
                continue;

            best = participant;
            bestScore = score;
        }

        if (best == null)
            best = _random.Pick(_participants);

        SetPrimaryTarget(drake, best.Value, now);
        return best;
    }

    private float ScoreTarget(
        LavalandAshDrakeComponent drake,
        EntityUid target,
        Vector2i? bossTile,
        Vector2i targetTile,
        TimeSpan now,
        bool applyCurrentPenalty)
    {
        var safeSeconds = drake.TargetPressureMemory.TotalSeconds;
        if (drake.LastPressureByTarget.TryGetValue(target, out var lastPressure))
            safeSeconds = Math.Clamp((now - lastPressure).TotalSeconds, 0, drake.TargetPressureMemory.TotalSeconds);

        var distancePenalty = bossTile == null
            ? 0f
            : ChebyshevDistance(bossTile.Value, targetTile) * 0.2f;
        var score = (float) safeSeconds - distancePenalty + _random.NextFloat(0f, 1.5f);

        if (applyCurrentPenalty &&
            drake.CurrentPrimaryTarget == target &&
            now - drake.LastTargetSwitchAt >= drake.TargetSwitchCooldown)
        {
            score -= 8f;
        }

        return score;
    }

    private void PruneTargetMemory(LavalandAshDrakeComponent drake)
    {
        if (drake.CurrentPrimaryTarget is { Valid: true } current &&
            !_participants.Contains(current))
        {
            drake.CurrentPrimaryTarget = null;
        }

        if (drake.LastPressureByTarget.Count == 0)
            return;

        foreach (var target in new List<EntityUid>(drake.LastPressureByTarget.Keys))
        {
            if (!_participants.Contains(target))
                drake.LastPressureByTarget.Remove(target);
        }
    }

    private static void SetPrimaryTarget(LavalandAshDrakeComponent drake, EntityUid target, TimeSpan now)
    {
        if (drake.CurrentPrimaryTarget == target)
            return;

        drake.CurrentPrimaryTarget = target;
        drake.LastTargetSwitchAt = now;
    }

    private float CalculateRage(EntityUid boss)
    {
        if (!TryComp<LavalandBossComponent>(boss, out _) ||
            !TryComp<DamageableComponent>(boss, out var damageable))
        {
            return 0f;
        }

        return Math.Clamp(Math.Max(0f, damageable.TotalDamage.Float()) / 50f, 0f, 20f);
    }

    private bool IsBelowHalfHealth(EntityUid boss)
    {
        if (!TryComp<LavalandBossComponent>(boss, out var bossComp) ||
            !TryComp<DamageableComponent>(boss, out var damageable))
        {
            return false;
        }

        return damageable.TotalDamage.Float() >= bossComp.MaxHealth * 0.5f;
    }

    private static TimeSpan GetScaledCooldown(TimeSpan baseCooldown, float rage)
    {
        return TimeSpan.FromSeconds(Math.Max(1.85, baseCooldown.TotalSeconds - rage * 0.04));
    }

    private void ProcessHealthPhases(
        EntityUid bossUid,
        LavalandAshDrakeComponent drake,
        LavalandBossComponent boss,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        if (!TryComp<DamageableComponent>(bossUid, out var damageable) || boss.MaxHealth <= 0)
            return;

        var healthFraction = Math.Clamp(1f - (float) damageable.TotalDamage / boss.MaxHealth, 0f, 1f);
        if (!drake.WhelpPhaseTriggered && healthFraction <= drake.WhelpPhaseHealthFraction)
        {
            drake.WhelpPhaseTriggered = true;
            SpawnWhelpWave(drake, arena, gridUid, grid, now);
            drake.NextWhelpWave = now + drake.WhelpWaveCooldown;
        }

        if (!drake.LavaPhaseTriggered && healthFraction <= drake.LavaPhaseHealthFraction)
        {
            drake.LavaPhaseTriggered = true;
            FillArenaPerimeter(drake, arena, gridUid, grid);
        }
    }

    private void SpawnWhelpWave(
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        TimeSpan now)
    {
        foreach (var participant in _participants)
        {
            var playerTile = GetEntityTile(participant, gridUid, grid);
            if (playerTile == null)
                continue;

            var offset = Cardinals[_random.Next(Cardinals.Length)] * drake.WhelpSpawnDistance;
            var spawnTile = ClampToInnerArena(arena, playerTile.Value + offset);
            var direction = new Vector2i(
                Math.Sign(playerTile.Value.X - spawnTile.X),
                Math.Sign(playerTile.Value.Y - spawnTile.Y));
            if (direction == Vector2i.Zero)
                direction = -Cardinals[_random.Next(Cardinals.Length)];

            var whelp = Spawn(drake.WhelpPrototype, _map.GridTileToLocal(gridUid, grid, spawnTile));
            drake.PhaseEntities.Add(whelp);
            _transform.SetLocalRotation(whelp, new Vector2(direction.X, direction.Y).ToAngle());
            if (TryComp(whelp, out TransformComponent? whelpXform) && !whelpXform.Anchored)
                _transform.AnchorEntity((whelp, whelpXform), (gridUid, grid), spawnTile);

            var attack = new LavalandAshDrakeWhelpAttack
            {
                Whelp = whelp,
                Grid = gridUid,
                AttackAt = now + drake.WhelpAttackDelay,
            };

            var perpendicular = new Vector2i(-direction.Y, direction.X);
            for (var step = 1; step <= drake.WhelpFireRange; step++)
            {
                var forward = spawnTile + direction * step;
                AddWhelpAttackTile(attack, drake, arena, gridUid, grid, forward);
                AddWhelpAttackTile(attack, drake, arena, gridUid, grid, forward + perpendicular);
                AddWhelpAttackTile(attack, drake, arena, gridUid, grid, forward - perpendicular);
            }

            drake.WhelpAttacks.Add(attack);
        }
    }

    private void AddWhelpAttackTile(
        LavalandAshDrakeWhelpAttack attack,
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i tile)
    {
        if (!IsInsideInnerArena(arena, tile) || !attack.Tiles.Add(tile))
            return;

        var telegraph = SpawnAnchored(drake.WhelpTargetPrototype, gridUid, grid, tile);
        if (telegraph != null)
            attack.Telegraphs.Add(telegraph.Value);
    }

    private void ProcessWhelpAttacks(
        LavalandAshDrakeComponent drake,
        MapGridComponent grid,
        TimeSpan now)
    {
        for (var i = drake.WhelpAttacks.Count - 1; i >= 0; i--)
        {
            var attack = drake.WhelpAttacks[i];
            if (now < attack.AttackAt)
                continue;

            foreach (var telegraph in attack.Telegraphs)
            {
                if (Exists(telegraph))
                    QueueDel(telegraph);
            }

            if (Exists(attack.Whelp))
            {
                if (TryComp(attack.Whelp, out TransformComponent? whelpXform) && whelpXform.Anchored)
                    _transform.Unanchor(attack.Whelp, whelpXform);

                foreach (var tile in attack.Tiles)
                    SpawnAnchored(drake.FirePrototype, attack.Grid, grid, tile);

                _audio.PlayPvs(drake.FireSound, attack.Whelp, AudioParams.Default.WithVolume(1f));
                QueueDel(attack.Whelp);
            }

            drake.WhelpAttacks.RemoveAt(i);
        }
    }

    private void FillArenaPerimeter(
        LavalandAshDrakeComponent drake,
        LavalandBossArenaComponent arena,
        EntityUid gridUid,
        MapGridComponent grid)
    {
        var (minX, maxX, minY, maxY) = GetInnerBounds(arena);
        var depth = Math.Max(1, drake.ArenaLavaDepth);
        var tiles = new HashSet<Vector2i>();
        for (var layer = 0; layer < depth; layer++)
        {
            var left = minX + layer;
            var right = maxX - layer;
            var bottom = minY + layer;
            var top = maxY - layer;
            if (left > right || bottom > top)
                break;

            for (var x = left; x <= right; x++)
            {
                tiles.Add(new Vector2i(x, bottom));
                tiles.Add(new Vector2i(x, top));
            }

            for (var y = bottom; y <= top; y++)
            {
                tiles.Add(new Vector2i(left, y));
                tiles.Add(new Vector2i(right, y));
            }
        }

        foreach (var tile in tiles)
        {
            var lava = SpawnAnchored(drake.CornerLavaPrototype, gridUid, grid, tile);
            if (lava != null)
                drake.PhaseEntities.Add(lava.Value);
        }
    }
    private Vector2i? GetEntityTile(EntityUid uid, EntityUid gridUid, MapGridComponent grid)
    {
        if (!uid.Valid ||
            !TryComp(uid, out TransformComponent? xform) ||
            xform.GridUid != gridUid)
        {
            return null;
        }

        return _map.LocalToTile(gridUid, grid, xform.Coordinates);
    }

    private bool IsDead(EntityUid uid)
    {
        return TryComp(uid, out MobStateComponent? mobState) && mobState.CurrentState == MobState.Dead;
    }

    private EntityUid? SpawnAnchored(string prototype, EntityUid gridUid, MapGridComponent grid, Vector2i index)
    {
        if (string.IsNullOrWhiteSpace(prototype))
            return null;

        var uid = Spawn(prototype, _map.GridTileToLocal(gridUid, grid, index));
        if (!TryComp(uid, out TransformComponent? xform) || xform.Anchored)
            return uid;

        _transform.AnchorEntity((uid, xform), (gridUid, grid), index);
        return uid;
    }

    private void ClearRuntimeState(EntityUid uid, LavalandAshDrakeComponent drake, bool restoreVisual)
    {
        drake.PendingTiles.Clear();
        drake.Swooping = false;
        drake.SwoopInvulnerable = false;
        drake.SwoopTarget = null;
        drake.SwoopImpactAt = TimeSpan.Zero;
        drake.PendingSwoops = 0;
        drake.NextQueuedSwoop = TimeSpan.Zero;
        drake.CurrentPrimaryTarget = null;
        drake.LastTargetSwitchAt = TimeSpan.Zero;
        drake.LastPressureByTarget.Clear();
        drake.NextWhelpWave = TimeSpan.Zero;
        drake.WhelpPhaseTriggered = false;
        drake.LavaPhaseTriggered = false;

        foreach (var attack in drake.WhelpAttacks)
        {
            foreach (var telegraph in attack.Telegraphs)
            {
                if (Exists(telegraph))
                    QueueDel(telegraph);
            }
        }
        drake.WhelpAttacks.Clear();

        foreach (var entity in drake.PhaseEntities)
        {
            if (Exists(entity))
                QueueDel(entity);
        }
        drake.PhaseEntities.Clear();

        foreach (var entity in drake.CageBorderEntities)
        {
            if (Exists(entity))
                QueueDel(entity);
        }
        drake.CageBorderEntities.Clear();

        foreach (var entity in drake.CageInteriorEntities)
        {
            if (Exists(entity))
                QueueDel(entity);
        }
        drake.CageInteriorEntities.Clear();

        if (drake.CageTargetEntity.HasValue && Exists(drake.CageTargetEntity.Value))
            QueueDel(drake.CageTargetEntity.Value);
        drake.CageTargetEntity = null;

        drake.CageActive = false;
        drake.CagePhase = 0;
        drake.CurrentCageTargetTile = null;
        drake.CageSafeTile = null;
        drake.NextCageFillAt = TimeSpan.Zero;

        if (!Exists(uid))
            return;

        if (restoreVisual)
            SetVisual(uid, LavalandAshDrakeVisualState.Dragon);

        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private static bool NeedsPressure(LavalandAshDrakeComponent drake, TimeSpan now)
    {
        return drake.LastPressureAt == TimeSpan.Zero ||
               now - drake.LastPressureAt >= drake.ForcePressureAfter;
    }

    private static void MarkPressure(LavalandAshDrakeComponent drake, TimeSpan now, string attackKind, EntityUid target)
    {
        drake.LastPressureAt = now;
        drake.LastAttackKind = attackKind;
        MarkTargetPressure(drake, target, now);
    }

    private static void MarkTargetPressure(LavalandAshDrakeComponent drake, EntityUid target, TimeSpan now)
    {
        if (!target.Valid)
            return;

        drake.LastPressureByTarget[target] = now;
    }

    private void SetVisual(EntityUid uid, LavalandAshDrakeVisualState state)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, LavalandAshDrakeVisuals.State, state, appearance);
    }

    private static Vector2i StepTowards(Vector2i from, Vector2i to)
    {
        return new Vector2i(
            from.X + Math.Sign(to.X - from.X),
            from.Y + Math.Sign(to.Y - from.Y));
    }

    private static int ChebyshevDistance(Vector2i a, Vector2i b)
    {
        return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }

    private static bool IsInsideInnerArena(LavalandBossArenaComponent arena, Vector2i tile)
    {
        var (minX, maxX, minY, maxY) = GetInnerBounds(arena);
        return tile.X >= minX && tile.X <= maxX && tile.Y >= minY && tile.Y <= maxY;
    }

    private static bool IsNearInnerArenaEdge(LavalandBossArenaComponent arena, Vector2i tile, int distance)
    {
        var (minX, maxX, minY, maxY) = GetInnerBounds(arena);
        return tile.X - minX <= distance ||
               maxX - tile.X <= distance ||
               tile.Y - minY <= distance ||
               maxY - tile.Y <= distance;
    }

    private static Vector2i ClampToInnerArena(LavalandBossArenaComponent arena, Vector2i tile)
    {
        var (minX, maxX, minY, maxY) = GetInnerBounds(arena);
        return new Vector2i(
            Math.Clamp(tile.X, minX, maxX),
            Math.Clamp(tile.Y, minY, maxY));
    }

    private static (int MinX, int MaxX, int MinY, int MaxY) GetInnerBounds(LavalandBossArenaComponent arena)
    {
        var halfWidth = arena.Width / 2;
        var halfHeight = arena.Height / 2;
        return (-halfWidth + 1, halfWidth - 1, -halfHeight + 1, halfHeight - 1);
    }
}
