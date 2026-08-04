// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.Polymorph.Systems;
using Content.Server.Stunnable;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.Lavaland;
using Content.Shared.DeadSpace.Lavaland.DrakeArmor;
using Content.Shared.Humanoid;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.DeadSpace.Lavaland;

public sealed class DrakeArmorSystem : EntitySystem
{
    private static readonly Vector2[] Cardinals =
    {
        new(0, 1),
        new(1, 0),
        new(0, -1),
        new(-1, 0),
    };

    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DrakeArmorTransformActionEvent>(OnTransform);
        SubscribeLocalEvent<DrakeFireBreathActionEvent>(OnFireWalls);
        SubscribeLocalEvent<DrakeFireRainActionEvent>(OnFireRain);
        SubscribeLocalEvent<DrakeSwoopActionEvent>(OnSwoop);
        SubscribeLocalEvent<DrakeArmorAbilityRuntimeComponent, BeforeDamageChangedEvent>(OnSwoopDamage);
        SubscribeLocalEvent<DrakeArmorAbilityRuntimeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<DrakeArmorAbilityRuntimeComponent>();
        while (query.MoveNext(out var uid, out var runtime))
        {
            ProcessPendingEffects(runtime, frameTime);
            ProcessSwoop(uid, runtime, frameTime);

            if (!runtime.Swooping && runtime.PendingEffects.Count == 0)
                RemCompDeferred<DrakeArmorAbilityRuntimeComponent>(uid);
        }
    }

    private void ProcessPendingEffects(DrakeArmorAbilityRuntimeComponent runtime, float frameTime)
    {
        for (var i = runtime.PendingEffects.Count - 1; i >= 0; i--)
        {
            var pending = runtime.PendingEffects[i];
            pending.Remaining -= frameTime;
            if (pending.Remaining > 0f)
                continue;

            if (pending.Marker is { } marker && Exists(marker))
                QueueDel(marker);
            Spawn(pending.Prototype, pending.Coordinates);
            runtime.PendingEffects.RemoveAt(i);
        }
    }

    private void ProcessSwoop(EntityUid uid, DrakeArmorAbilityRuntimeComponent runtime, float frameTime)
    {
        if (!runtime.Swooping)
            return;

        runtime.SwoopTimer -= frameTime;
        if (runtime.SwoopTimer > 0f)
            return;

        switch (runtime.SwoopPhase)
        {
            case DrakeArmorSwoopPhase.Windup:
                runtime.SwoopPhase = DrakeArmorSwoopPhase.Travel;
                runtime.SwoopTimer = 0f;
                break;
            case DrakeArmorSwoopPhase.Travel:
                StepSwoop(uid, runtime);
                break;
            case DrakeArmorSwoopPhase.Recover:
                FinishSwoop(uid, runtime);
                break;
        }
    }

    private void StepSwoop(EntityUid uid, DrakeArmorAbilityRuntimeComponent runtime)
    {
        var current = _transform.GetMapCoordinates(uid);
        if (current.MapId != runtime.SwoopTarget.MapId)
        {
            FinishSwoop(uid, runtime);
            return;
        }

        var delta = runtime.SwoopTarget.Position - current.Position;
        if (Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) <= 0.05f)
        {
            BeginSwoopImpact(uid, runtime, current);
            return;
        }

        var step = new Vector2(
            Math.Sign(delta.X) * Math.Min(Math.Abs(delta.X), 1f),
            Math.Sign(delta.Y) * Math.Min(Math.Abs(delta.Y), 1f));
        var next = new MapCoordinates(current.Position + step, current.MapId);
        _transform.SetMapCoordinates(uid, next);
        SetVisual(uid, LavalandAshDrakeVisualState.Shadow);
        runtime.SwoopTimer = (float) runtime.SwoopStepDelay.TotalSeconds;

        if (Vector2.DistanceSquared(next.Position, runtime.SwoopTarget.Position) <= 0.0025f)
            BeginSwoopImpact(uid, runtime, next);
    }

    private void BeginSwoopImpact(EntityUid uid, DrakeArmorAbilityRuntimeComponent runtime, MapCoordinates coordinates)
    {
        if (runtime.SwoopPhase == DrakeArmorSwoopPhase.Recover)
            return;

        Spawn("LavalandAshDrakeLanding", coordinates);
        SetVisual(uid, LavalandAshDrakeVisualState.Swoop);
        runtime.SwoopPhase = DrakeArmorSwoopPhase.Recover;
        runtime.SwoopTimer = (float) runtime.SwoopRecover.TotalSeconds;
    }

    private void FinishSwoop(EntityUid uid, DrakeArmorAbilityRuntimeComponent runtime)
    {
        var center = _transform.GetMapCoordinates(uid);
        _audio.PlayPvs(runtime.SwoopImpactSound, uid);
        foreach (var victim in _lookup.GetEntitiesInRange(center, runtime.SwoopRadius))
        {
            if (victim != uid)
                _damage.TryChangeDamage(victim, runtime.SwoopDamage, origin: uid);
        }

        runtime.Swooping = false;
        SetVisual(uid, LavalandAshDrakeVisualState.Dragon);
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void OnTransform(DrakeArmorTransformActionEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<HumanoidAppearanceComponent>(args.Performer, out var appearance) &&
            args.BlockedSpecies.Contains(appearance.Species))
        {
            // Leave Handled false so a forbidden species does not spend the action cooldown.
            return;
        }
        // Skeletons use the same 50/50 roll: Drake on success, thermal damage on failure.
        if (_random.Prob(args.DrakeChance))
        {
            args.Handled = _polymorph.PolymorphEntity(args.Performer, args.DrakePolymorph) != null;
            return;
        }

        if (TryComp<HumanoidAppearanceComponent>(args.Performer, out var humanoid) && humanoid.Species == "Skeleton")
        {
            _damage.TryChangeDamage(args.Performer, args.RepeatedSkeletonDamage, true, origin: args.Performer);
            args.Handled = true;
            return;
        }

        var skeleton = _polymorph.PolymorphEntity(args.Performer, args.SkeletonPolymorph);
        if (skeleton is not { } skeletonUid)
            return;

        _stun.TryKnockdown(skeletonUid, args.SkeletonStunDuration, autoStand: true, drop: false, force: true);
        _chat.TryEmoteWithChat(skeletonUid, "Scream", ignoreActionBlocker: true, forceEmote: true);
        args.Handled = true;
    }

    private void OnFireWalls(DrakeFireBreathActionEvent args)
    {
        if (args.Handled)
            return;

        var origin = _transform.GetMapCoordinates(args.Performer);
        var runtime = EnsureComp<DrakeArmorAbilityRuntimeComponent>(args.Performer);
        foreach (var direction in Cardinals)
        {
            for (var step = 1; step <= args.Range; step++)
            {
                runtime.PendingEffects.Add(new DrakeArmorPendingEffect
                {
                    Coordinates = new MapCoordinates(origin.Position + direction * step, origin.MapId),
                    Prototype = args.FirePrototype,
                    Remaining = (float) (args.StepDelay.TotalSeconds * step),
                });
            }
        }

        _audio.PlayPvs(args.Sound, args.Performer);
        args.Handled = true;
    }

    private void OnFireRain(DrakeFireRainActionEvent args)
    {
        if (args.Handled)
            return;

        var target = _transform.ToMapCoordinates(args.Target);
        var runtime = EnsureComp<DrakeArmorAbilityRuntimeComponent>(args.Performer);
        for (var x = -args.Radius; x <= args.Radius; x++)
        {
            for (var y = -args.Radius; y <= args.Radius; y++)
            {
                var coordinates = target.Offset(new Vector2(x, y));
                runtime.PendingEffects.Add(new DrakeArmorPendingEffect
                {
                    Coordinates = coordinates,
                    Prototype = args.FirePrototype,
                    Marker = Spawn(args.TargetPrototype, coordinates),
                    Remaining = (float) args.Delay.TotalSeconds,
                });
            }
        }

        _audio.PlayPvs(args.Sound, args.Target);
        args.Handled = true;
    }

    private void OnSwoop(DrakeSwoopActionEvent args)
    {
        if (args.Handled)
            return;

        var origin = _transform.GetMapCoordinates(args.Performer);
        var target = _transform.ToMapCoordinates(args.Target);
        if (origin.MapId != target.MapId || Vector2.Distance(origin.Position, target.Position) > args.MaxRange)
            return;

        var runtime = EnsureComp<DrakeArmorAbilityRuntimeComponent>(args.Performer);
        if (runtime.Swooping)
            return;

        runtime.Swooping = true;
        runtime.SwoopPhase = DrakeArmorSwoopPhase.Windup;
        runtime.SwoopTarget = target;
        runtime.SwoopTimer = (float) args.Windup.TotalSeconds;
        runtime.SwoopStepDelay = args.StepDelay;
        runtime.SwoopRecover = args.Recover;
        runtime.SwoopRadius = args.Radius;
        runtime.SwoopDamage = args.Damage;
        runtime.SwoopImpactSound = args.ImpactSound;
        SetVisual(args.Performer, LavalandAshDrakeVisualState.Shadow);
        _movement.RefreshMovementSpeedModifiers(args.Performer);
        _audio.PlayPvs(args.WindupSound, args.Performer);
        args.Handled = true;
    }

    private void OnRefreshMovementSpeed(
        EntityUid uid,
        DrakeArmorAbilityRuntimeComponent runtime,
        RefreshMovementSpeedModifiersEvent args)
    {
        if (runtime.Swooping)
            args.ModifySpeed(0f, 0f);
    }
    private void OnSwoopDamage(Entity<DrakeArmorAbilityRuntimeComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (ent.Comp.Swooping)
            args.Cancelled = true;
    }

    private void SetVisual(EntityUid uid, LavalandAshDrakeVisualState state)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, LavalandAshDrakeVisuals.State, state, appearance);
    }
}

[RegisterComponent]
public sealed partial class DrakeArmorAbilityRuntimeComponent : Component
{
    public readonly List<DrakeArmorPendingEffect> PendingEffects = new();
    public bool Swooping;
    public DrakeArmorSwoopPhase SwoopPhase;
    public MapCoordinates SwoopTarget;
    public float SwoopTimer;
    public TimeSpan SwoopStepDelay;
    public TimeSpan SwoopRecover;
    public float SwoopRadius;
    public DamageSpecifier SwoopDamage = new();
    public SoundSpecifier SwoopImpactSound = new SoundPathSpecifier("/Audio/_DeadSpace/Lavaland/AshDrake/meteorimpact.ogg");
}

public sealed class DrakeArmorPendingEffect
{
    public MapCoordinates Coordinates;
    public EntProtoId Prototype;
    public EntityUid? Marker;
    public float Remaining;
}

public enum DrakeArmorSwoopPhase : byte
{
    Windup,
    Travel,
    Recover,
}