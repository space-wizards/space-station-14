using System.Numerics;
using Content.Server.DeadSpace.Lavaland.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage.Components;
using Content.Shared.DeadSpace.Lavaland;
using Content.Shared.Effects;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Reflect;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Lavaland;

public sealed class LavalandColossusSystem : EntitySystem
{
    private static readonly float[] ShotgunOffsets = [12.5f, 7.5f, 2.5f, -2.5f, -7.5f, -12.5f];
    private static readonly float[] EnragedShotgunOffsets = [17.5f, 12.5f, 7.5f, 2.5f, -2.5f, -7.5f, -12.5f, -17.5f];
    private static readonly float[] Cardinals = [0f, 90f, 180f, 270f];
    private static readonly float[] Diagonals = [45f, 135f, 225f, 315f];
    private static readonly float[] AllDirections = [0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f];

    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _camera = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<EntityUid> _participants = new();
    private readonly List<EntityUid> _telegraphTargets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LavalandColossusComponent, LavalandBossFightStartedEvent>(OnFightStarted);
        SubscribeLocalEvent<LavalandColossusComponent, LavalandBossResetEvent>(OnReset);
        SubscribeLocalEvent<LavalandColossusComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LavalandColossusComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<LavalandColossusProjectileComponent, ComponentShutdown>(OnProjectileShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<LavalandColossusComponent, LavalandBossComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var colossus, out var boss, out var xform))
        {
            if (boss.Arena is not { Valid: true } arenaUid ||
                !TryComp<LavalandBossArenaComponent>(arenaUid, out var arena) ||
                arena.Ended ||
                !arena.FightStarted ||
                xform.GridUid != arena.Grid ||
                !TryComp<MapGridComponent>(arena.Grid, out var grid) ||
                IsDead(uid))
            {
                SetEnraged(uid, colossus, false);
                ClearRuntime(colossus);
                DeleteProjectiles(colossus);
                continue;
            }

            CollectParticipants(arena);
            if (_participants.Count == 0)
            {
                SetEnraged(uid, colossus, false);
                ClearRuntime(colossus);
                colossus.NextAttack = now + TimeSpan.FromSeconds(1);
                DeleteProjectiles(colossus);
                continue;
            }

            ProcessPendingShots(uid, colossus, now);
            if (colossus.BusyUntil > now || colossus.NextAttack > now)
                continue;

            var target = PickTarget(uid);
            if (target == null)
                continue;

            RunAttack(uid, colossus, arena, grid, target.Value, now);
        }
    }

    private void OnFightStarted(Entity<LavalandColossusComponent> ent, ref LavalandBossFightStartedEvent args)
    {
        PrepareFight(ent.Owner, ent.Comp);
        Popup(ent.Owner, "lavaland-colossus-speech-see-you");
    }

    private void OnReset(Entity<LavalandColossusComponent> ent, ref LavalandBossResetEvent args)
    {
        DeleteProjectiles(ent.Comp);
        PrepareFight(ent.Owner, ent.Comp);
    }

    private void OnShutdown(Entity<LavalandColossusComponent> ent, ref ComponentShutdown args)
    {
        DeleteProjectiles(ent.Comp);
    }

    private void PrepareFight(EntityUid uid, LavalandColossusComponent colossus)
    {
        SetEnraged(uid, colossus, false);
        ClearRuntime(colossus);
        colossus.NextAttack = _timing.CurTime + TimeSpan.FromSeconds(1);
    }

    private void RunAttack(
        EntityUid uid,
        LavalandColossusComponent colossus,
        LavalandBossArenaComponent arena,
        MapGridComponent grid,
        EntityUid target,
        TimeSpan now)
    {
        if (!TryGetDirection(uid, target, out var targetAngle))
        {
            colossus.NextAttack = now + TimeSpan.FromSeconds(0.5);
            return;
        }

        if (HasComp<ReflectComponent>(target))
        {
            if (!colossus.Enraged)
                Popup(uid, "lavaland-colossus-speech-cannot-dodge");

            SetEnraged(uid, colossus, true);
            Telegraph(uid, colossus);
            QueueVolley(colossus, AllDirections, now);
            colossus.NextAttack = now + GetScaledCooldown(colossus.RandomCooldown, 1f, colossus.MaxCooldownReduction);
            return;
        }

        SetEnraged(uid, colossus, false);

        var anger = GetAnger(uid, arena);
        var rage = anger / 20f;
        colossus.NextAttack = now + GetScaledCooldown(colossus.DefaultCooldown, rage, colossus.MaxCooldownReduction);
        if (_random.Prob(Math.Clamp(colossus.MajorAttackChance + anger / 100f, 0f, 1f)))
        {
            Telegraph(uid, colossus);
            if (IsBelowThird(uid, arena))
            {
                Popup(uid, "lavaland-colossus-speech-die");
                QueueSpiral(colossus, now + colossus.DoubleSpiralWindup, false, 8);
                QueueSpiral(colossus, now + colossus.DoubleSpiralWindup, true, 8);
                colossus.BusyUntil = now + colossus.DoubleSpiralWindup +
                                     TimeSpan.FromSeconds(colossus.SpiralStepDelay.TotalSeconds * colossus.SpiralShots);
            }
            else
            {
                Popup(uid, "lavaland-colossus-speech-judgement");
                QueueSpiral(colossus, now, _random.Prob(0.5f), 8);
                colossus.BusyUntil = now + TimeSpan.FromSeconds(colossus.SpiralStepDelay.TotalSeconds * colossus.SpiralShots);
            }
            return;
        }

        if (_random.Prob(colossus.RandomAttackChance))
        {
            QueueRandomShots(uid, colossus, arena, grid, now);
            colossus.NextAttack = now + GetScaledCooldown(colossus.RandomCooldown, rage, colossus.MaxCooldownReduction);
            return;
        }

        if (_random.Prob(colossus.ShotgunChance))
        {
            QueueShotgun(colossus, targetAngle, IsBelowTwoThirds(uid, arena), now);

            PushBack(uid, -targetAngle.ToVec());
            colossus.NextAttack = now + GetScaledCooldown(colossus.ShotgunCooldown, rage, colossus.MaxCooldownReduction);
            return;
        }

        QueueVolley(colossus, Diagonals, now);
        QueueVolley(colossus, Cardinals, now + colossus.AlternatingStepDelay);
        QueueVolley(colossus, Diagonals, now + colossus.AlternatingStepDelay * 2);
        QueueVolley(colossus, Cardinals, now + colossus.AlternatingStepDelay * 3);
        colossus.BusyUntil = now + colossus.AlternatingStepDelay * 3;
        colossus.NextAttack = now + GetScaledCooldown(colossus.AlternatingCooldown, rage, colossus.MaxCooldownReduction);
    }

    private void QueueRandomShots(
        EntityUid uid,
        LavalandColossusComponent colossus,
        LavalandBossArenaComponent arena,
        MapGridComponent grid,
        TimeSpan now)
    {
        if (Transform(uid).GridUid is not { Valid: true } gridUid)
            return;

        var center = _map.LocalToTile(gridUid, grid, Transform(uid).Coordinates);
        TryPlayShotSound(uid, colossus, now, 0f);
        for (var x = center.X - colossus.RandomShotRadius; x <= center.X + colossus.RandomShotRadius; x++)
        {
            for (var y = center.Y - colossus.RandomShotRadius; y <= center.Y + colossus.RandomShotRadius; y++)
            {
                var tile = new Vector2i(x, y);
                if (tile == center || !IsInsideArena(arena, tile) || !_random.Prob(colossus.RandomShotChance))
                    continue;

                var direction = (Vector2) (tile - center);
                QueueShot(colossus, direction.ToAngle(), now);
            }
        }
    }

    internal static void QueueSpiral(
        LavalandColossusComponent colossus,
        TimeSpan startsAt,
        bool negative,
        int counterStart)
    {
        var counter = counterStart;
        for (var i = 0; i < colossus.SpiralShots; i++)
        {
            counter += negative ? -1 : 1;
            if (counter > 16)
                counter = 1;
            else if (counter < 1)
                counter = 16;

            QueueShot(
                colossus,
                Angle.FromDegrees(counter * 22.5f),
                startsAt + TimeSpan.FromSeconds(colossus.SpiralStepDelay.TotalSeconds * i),
                true);
        }
    }

    internal static void QueueVolley(LavalandColossusComponent colossus, float[] directions, TimeSpan fireAt)
    {
        for (var i = 0; i < directions.Length; i++)
            QueueShot(colossus, Angle.FromDegrees(directions[i]), fireAt, i == 0);
    }

    internal static void QueueShotgun(
        LavalandColossusComponent colossus,
        Angle targetAngle,
        bool enraged,
        TimeSpan fireAt)
    {
        var offsets = enraged ? EnragedShotgunOffsets : ShotgunOffsets;
        for (var i = 0; i < offsets.Length; i++)
            QueueShot(colossus, targetAngle + Angle.FromDegrees(offsets[i]), fireAt, i == 0);
    }

    private static void QueueShot(
        LavalandColossusComponent colossus,
        Angle angle,
        TimeSpan fireAt,
        bool playSound = false)
    {
        colossus.PendingShots.Add(new LavalandColossusPendingShot
        {
            Angle = angle,
            FireAt = fireAt,
            PlaySound = playSound,
        });
    }

    private void ProcessPendingShots(EntityUid uid, LavalandColossusComponent colossus, TimeSpan now)
    {
        for (var i = colossus.PendingShots.Count - 1; i >= 0; i--)
        {
            var shot = colossus.PendingShots[i];
            if (shot.FireAt > now)
                continue;

            Fire(uid, colossus, shot.Angle, shot.PlaySound);
            colossus.PendingShots.RemoveAt(i);
        }
    }

    private void Fire(EntityUid uid, LavalandColossusComponent colossus, Angle angle, bool playSound)
    {
        var projectile = Spawn(colossus.ProjectilePrototype, Transform(uid).Coordinates);
        var marker = EnsureComp<LavalandColossusProjectileComponent>(projectile);
        marker.Boss = uid;
        colossus.ActiveProjectiles.Add(projectile);
        _gun.ShootProjectile(projectile, angle.ToVec(), Vector2.Zero, uid, uid, GetProjectileSpeed(uid, colossus));
        if (playSound)
            TryPlayShotSound(uid, colossus, _timing.CurTime, -6f);
    }

    private void OnProjectileShutdown(Entity<LavalandColossusProjectileComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<LavalandColossusComponent>(ent.Comp.Boss, out var colossus))
            colossus.ActiveProjectiles.Remove(ent.Owner);
    }

    private void Telegraph(EntityUid uid, LavalandColossusComponent colossus)
    {
        _telegraphTargets.Clear();
        var bossPosition = _transform.GetWorldPosition(uid);
        foreach (var participant in _participants)
        {
            if (Vector2.DistanceSquared(_transform.GetWorldPosition(participant), bossPosition) > 100f)
                continue;

            _telegraphTargets.Add(participant);
            _camera.KickCamera(participant, _random.NextAngle().ToVec() * 0.16f);
        }

        if (_telegraphTargets.Count > 0)
            _color.RaiseEffect(Color.FromHex("#c80000"), _telegraphTargets, Filter.Pvs(uid, entityManager: EntityManager));

        _audio.PlayPvs(colossus.TelegraphSound, uid, AudioParams.Default.WithVolume(2f));
    }

    private void CollectParticipants(LavalandBossArenaComponent arena)
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
    }

    private EntityUid? PickTarget(EntityUid uid)
    {
        EntityUid? closest = null;
        var closestDistance = float.MaxValue;
        var origin = _transform.GetWorldPosition(uid);
        foreach (var participant in _participants)
        {
            var distance = Vector2.DistanceSquared(origin, _transform.GetWorldPosition(participant));
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closest = participant;
        }

        return closest;
    }

    private bool TryGetDirection(EntityUid source, EntityUid target, out Angle angle)
    {
        var direction = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(source);
        if (direction.LengthSquared() < 0.001f)
        {
            angle = Angle.Zero;
            return false;
        }

        angle = direction.ToAngle();
        return true;
    }

    private void PushBack(EntityUid uid, Vector2 direction)
    {
        if (TryComp<PhysicsComponent>(uid, out var body))
            _physics.ApplyLinearImpulse(uid, direction.Normalized() * body.Mass * 0.8f, body: body);
    }

    private void TryPlayShotSound(
        EntityUid uid,
        LavalandColossusComponent colossus,
        TimeSpan now,
        float volume)
    {
        if (now < colossus.NextShotSound)
            return;

        var soundParams = AudioParams.Default.WithVolume(volume);
        soundParams.Variation = 0.05f;
        _audio.PlayPvs(colossus.ShotSound, uid, soundParams);
        colossus.NextShotSound = now + colossus.ShotSoundInterval;
    }

    private void OnRefreshMovementSpeed(
        Entity<LavalandColossusComponent> ent,
        ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Enraged)
            args.ModifySpeed(ent.Comp.EnragedMovementSpeedMultiplier, ent.Comp.EnragedMovementSpeedMultiplier);
    }

    private void SetEnraged(EntityUid uid, LavalandColossusComponent colossus, bool enraged)
    {
        if (colossus.Enraged == enraged)
            return;

        colossus.Enraged = enraged;
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private float GetAnger(EntityUid uid, LavalandBossArenaComponent arena)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return 0f;

        var scaledMax = Math.Max(1f, arena.ScaledMaxHealth);
        var baseDamage = damageable.TotalDamage.Float() / scaledMax * 2500f;
        return Math.Clamp(baseDamage / 50f, 0f, 20f);
    }

    private float GetProjectileSpeed(EntityUid uid, LavalandColossusComponent colossus)
    {
        if (!TryComp<LavalandBossComponent>(uid, out var boss) ||
            boss.Arena is not { Valid: true } arenaUid ||
            !TryComp<LavalandBossArenaComponent>(arenaUid, out var arena))
        {
            return colossus.ProjectileSpeed;
        }

        var rage = GetAnger(uid, arena) / 20f;
        return GetScaledProjectileSpeed(colossus.ProjectileSpeed, colossus.RageProjectileSpeedBonus, rage);
    }

    internal static float GetScaledProjectileSpeed(float baseSpeed, float rageBonus, float rage)
    {
        return Math.Max(0.1f, baseSpeed + Math.Max(0f, rageBonus) * Math.Clamp(rage, 0f, 1f));
    }

    internal static TimeSpan GetScaledCooldown(TimeSpan cooldown, float rage, float maxReduction)
    {
        var reduction = Math.Clamp(maxReduction, 0f, 0.8f) * Math.Clamp(rage, 0f, 1f);
        return TimeSpan.FromSeconds(Math.Max(0.1, cooldown.TotalSeconds * (1f - reduction)));
    }

    private bool IsBelowThird(EntityUid uid, LavalandBossArenaComponent arena)
    {
        return TryComp<DamageableComponent>(uid, out var damageable) &&
               damageable.TotalDamage.Float() > Math.Max(1f, arena.ScaledMaxHealth) * 2f / 3f;
    }

    private bool IsBelowTwoThirds(EntityUid uid, LavalandBossArenaComponent arena)
    {
        return TryComp<DamageableComponent>(uid, out var damageable) &&
               damageable.TotalDamage.Float() > Math.Max(1f, arena.ScaledMaxHealth) / 3f;
    }

    internal static bool IsInsideArena(LavalandBossArenaComponent arena, Vector2i tile)
    {
        var halfWidth = arena.Width / 2;
        var halfHeight = arena.Height / 2;
        return tile.X > -halfWidth && tile.X < halfWidth && tile.Y > -halfHeight && tile.Y < halfHeight;
    }

    private bool IsDead(EntityUid uid)
    {
        return TryComp<MobStateComponent>(uid, out var mob) && mob.CurrentState == MobState.Dead;
    }

    private void Popup(EntityUid uid, string message)
    {
        _popup.PopupEntity(
            Loc.GetString(message),
            uid,
            Filter.Pvs(uid, entityManager: EntityManager),
            true,
            PopupType.LargeCaution);
    }

    private void ClearRuntime(LavalandColossusComponent colossus)
    {
        colossus.PendingShots.Clear();
        colossus.BusyUntil = TimeSpan.Zero;
        colossus.NextShotSound = TimeSpan.Zero;
    }

    private void DeleteProjectiles(LavalandColossusComponent colossus)
    {
        foreach (var projectile in colossus.ActiveProjectiles)
        {
            if (Exists(projectile))
                QueueDel(projectile);
        }

        colossus.ActiveProjectiles.Clear();
    }
}
