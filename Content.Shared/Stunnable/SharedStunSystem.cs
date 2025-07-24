using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Stunnable;

public abstract partial class SharedStunSystem : EntitySystem
{
    public static readonly EntProtoId StunId = "StatusEffectStunned";
    public static readonly EntProtoId KnockdownId = "StatusEffectKnockdown";

    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly ActionBlockerSystem Blocker = default!;
    [Dependency] protected readonly AlertsSystem Alerts = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected readonly SharedStaminaSystem Stamina = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StunnedComponent, ComponentStartup>(UpdateCanMove);
        SubscribeLocalEvent<StunnedComponent, ComponentShutdown>(OnStunShutdown);

        SubscribeLocalEvent<StunOnContactComponent, StartCollideEvent>(OnStunOnContactCollide);

        // Attempt event subscriptions.
        SubscribeLocalEvent<StunnedComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<StunnedComponent, InteractionAttemptEvent>(OnAttemptInteract);
        SubscribeLocalEvent<StunnedComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, DropAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<StunnedComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<MobStateComponent, MobStateChangedEvent>(OnMobStateChanged);

        // New Status Effect subscriptions
        SubscribeLocalEvent<StunnedStatusEffectComponent, StatusEffectAppliedEvent>(OnStunEffectApplied);
        SubscribeLocalEvent<StunnedStatusEffectComponent, StatusEffectRemovedEvent>(OnStunStatusRemoved);
        SubscribeLocalEvent<StunnedStatusEffectComponent, StatusEffectRelayedEvent<StunEndAttemptEvent>>(OnStunEndAttempt);

        SubscribeLocalEvent<KnockdownStatusEffectComponent, StatusEffectRelayedEvent<StandUpAttemptEvent>>(OnStandUpAttempt);

        // Stun Appearance Data
        InitializeKnockdown();
        InitializeAppearance();
    }

    private void OnAttemptInteract(Entity<StunnedComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMobStateChanged(EntityUid uid, MobStateComponent component, MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Alive:
                {
                    break;
                }
            case MobState.Critical:
                {
                    _status.TryRemoveStatusEffect(uid, StunId);
                    break;
                }
            case MobState.Dead:
                {
                    _status.TryRemoveStatusEffect(uid, StunId);
                    break;
                }
            case MobState.Invalid:
            default:
                return;
        }

    }

    private void OnStunShutdown(Entity<StunnedComponent> ent, ref ComponentShutdown args)
    {
        // This exists so the client can end their funny animation if they're playing one.
        UpdateCanMove(ent, ent.Comp, args);
        Appearance.RemoveData(ent, StunVisuals.SeeingStars);
    }

    private void UpdateCanMove(EntityUid uid, StunnedComponent component, EntityEventArgs args)
    {
        Blocker.UpdateCanMove(uid);
    }

    private void OnStunOnContactCollide(Entity<StunOnContactComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        if (_entityWhitelist.IsBlacklistPass(ent.Comp.Blacklist, args.OtherEntity))
            return;

        TryUpdateStunDuration(args.OtherEntity, ent.Comp.Duration);
        TryKnockdown(args.OtherEntity, ent.Comp.Duration, true, force: true);
    }

    // TODO STUN: Make events for different things. (Getting modifiers, attempt events, informative events...)
    public bool TryAddStunDuration(EntityUid uid, TimeSpan duration)
    {
        if (!_status.TryAddStatusEffectDuration(uid, StunId, duration))
            return false;

        OnStunnedSuccessfully(uid, duration);
        return true;
    }

    public bool TryUpdateStunDuration(EntityUid uid, TimeSpan? duration)
    {
        if (!_status.TryUpdateStatusEffectDuration(uid, StunId, duration))
            return false;

        OnStunnedSuccessfully(uid, duration);
        return true;
    }

    private void OnStunnedSuccessfully(EntityUid uid, TimeSpan? duration)
    {
        var ev = new StunnedEvent(); // todo: rename event or change how it is raised - this event is raised each time duration of stun was externally changed
        RaiseLocalEvent(uid, ref ev);

        var timeForLogs = duration.HasValue
            ? duration.Value.Seconds.ToString()
            : "Infinite";
        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} stunned for {timeForLogs} seconds");
    }

    public bool TryAddKnockdownDuration(EntityUid uid, TimeSpan duration)
    {
        if (!_status.TryAddStatusEffectDuration(uid, KnockdownId, duration))
            return false;

        TryKnockdown(uid, duration, true, force: true);

        return true;

    }

    public bool TryUpdateKnockdownDuration(EntityUid uid, TimeSpan? duration)
    {
        if (!_status.TryUpdateStatusEffectDuration(uid, KnockdownId, duration))
            return false;

        return TryKnockdown(uid, duration, true, force: true);
    }

    /// <summary>
    ///     Knocks down the entity, making it fall to the ground.
    /// </summary>
    public bool TryKnockdown(Entity<StandingStateComponent?> entity, TimeSpan? time, bool refresh, bool autoStand = true, bool drop = true, bool force = false)
    {
        if (time <= TimeSpan.Zero)
            return false;

        // Can't fall down if you can't actually be downed.
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        if (!force)
        {
            var evAttempt = new KnockDownAttemptEvent(autoStand, drop);
            RaiseLocalEvent(entity, ref evAttempt);

            if (evAttempt.Cancelled)
                return false;

            autoStand = evAttempt.AutoStand;
            drop = evAttempt.Drop;
        }

        Knockdown(entity!, time, refresh, autoStand, drop);

        return true;
    }

    private void Knockdown(Entity<StandingStateComponent> entity, TimeSpan? time, bool refresh, bool autoStand, bool drop)
    {
        // Initialize our component with the relevant data we need if we don't have it
        if (EnsureComp<KnockedDownComponent>(entity, out var component))
        {
            RefreshKnockedMovement((entity, component));
            CancelKnockdownDoAfter((entity, component));
        }
        else
        {
            // Only drop items the first time we want to fall...
            if (drop)
            {
                var ev = new DropHandItemsEvent();
                RaiseLocalEvent(entity, ref ev);
            }

            // Only update Autostand value if it's our first time being knocked down...
            SetAutoStand((entity, component), autoStand);
        }

        var knockedEv = new KnockedDownEvent(time);
        RaiseLocalEvent(entity, ref knockedEv);

        if (time != null)
        {
            UpdateKnockdownTime((entity, component), time.Value, refresh);
            _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(entity):user} knocked down for {time.Value.Seconds} seconds");
        }
        else
            _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(entity):user} knocked down for an indefinite amount of time");

        Alerts.ShowAlert(entity, KnockdownAlert, null, (GameTiming.CurTime, component.NextUpdate));
    }

    public bool TryAddParalyzeDuration(EntityUid uid, TimeSpan duration)
    {
        var knockdown = TryAddKnockdownDuration(uid, duration);
        var stunned = TryAddStunDuration(uid, duration);

        return knockdown || stunned;
    }

    public bool TryUpdateParalyzeDuration(EntityUid uid, TimeSpan? duration)
    {
        var knockdown = TryUpdateKnockdownDuration(uid, duration);
        var stunned = TryUpdateStunDuration(uid, duration);

        return knockdown || stunned;
    }

    public bool TryUnstun(Entity<StunnedComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return true;

        var ev = new StunEndAttemptEvent();
        RaiseLocalEvent(entity, ref ev);

        return !ev.Cancelled && RemComp<StunnedComponent>(entity);
    }

    private void OnStunEffectApplied(Entity<StunnedStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        EnsureComp<StunnedComponent>(args.Target);
    }

    private void OnStunStatusRemoved(Entity<StunnedStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        TryUnstun(args.Target);
    }

    private void OnStunEndAttempt(Entity<StunnedStatusEffectComponent> entity, ref StatusEffectRelayedEvent<StunEndAttemptEvent> args)
    {
        if (args.Args.Cancelled)
            return;

        var ev = args.Args;
        ev.Cancelled = true;
        args.Args = ev;
    }

    private void OnStandUpAttempt(Entity<KnockdownStatusEffectComponent> entity, ref StatusEffectRelayedEvent<StandUpAttemptEvent> args)
    {
        if (args.Args.Cancelled)
            return;

        var ev = args.Args;
        ev.Cancelled = true;
        args.Args = ev;
    }

    #region Attempt Event Handling

    private void OnMoveAttempt(EntityUid uid, StunnedComponent stunned, UpdateCanMoveEvent args)
    {
        if (stunned.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void OnAttempt(EntityUid uid, StunnedComponent stunned, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    private void OnEquipAttempt(EntityUid uid, StunnedComponent stunned, IsEquippingAttemptEvent args)
    {
        // is this a self-equip, or are they being stripped?
        if (args.Equipee == uid)
            args.Cancel();
    }

    private void OnUnequipAttempt(EntityUid uid, StunnedComponent stunned, IsUnequippingAttemptEvent args)
    {
        // is this a self-equip, or are they being stripped?
        if (args.Unequipee == uid)
            args.Cancel();
    }

    #endregion
}
