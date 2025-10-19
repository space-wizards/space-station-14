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
        SubscribeLocalEvent<StunnedStatusEffectComponent, StatusEffectAppliedEvent>(OnStunStatusApplied);
        SubscribeLocalEvent<StunnedStatusEffectComponent, StatusEffectRemovedEvent>(OnStunStatusRemoved);
        SubscribeLocalEvent<StunnedStatusEffectComponent, StatusEffectRelayedEvent<StunEndAttemptEvent>>(OnStunEndAttempt);

        SubscribeLocalEvent<KnockdownStatusEffectComponent, StatusEffectAppliedEvent>(OnKnockdownStatusApplied);
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
        TryKnockdown(args.OtherEntity, ent.Comp.Duration, force: true);
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

    /// <summary>
    ///     Tries to knock an entity to the ground, but will fail if they aren't able to crawl.
    ///     Useful if you don't want to paralyze an entity that can't crawl, but still want to knockdown
    ///     entities that can.
    /// </summary>
    /// <param name="entity">Entity we're trying to knockdown.</param>
    /// <param name="time">Time of the knockdown.</param>
    /// <param name="refresh">Do we refresh their timer, or add to it if one exists?</param>
    /// <param name="autoStand">Whether we should automatically stand when knockdown ends.</param>
    /// <param name="drop">Should we drop what we're holding?</param>
    /// <param name="force">Should we force crawling? Even if something tried to block it?</param>
    /// <returns>Returns true if the entity is able to crawl, and was able to be knocked down.</returns>
    public bool TryCrawling(Entity<CrawlerComponent?> entity,
        TimeSpan? time,
        bool refresh = true,
        bool autoStand = true,
        bool drop = true,
        bool force = false)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        return TryKnockdown(entity, time, refresh, autoStand, drop, force);
    }

    /// <inheritdoc cref="TryCrawling(Entity{CrawlerComponent?},TimeSpan?,bool,bool,bool,bool)"/>
    /// <summary>An overload of TryCrawling which uses the default crawling time from the CrawlerComponent as its timespan.</summary>
    public bool TryCrawling(Entity<CrawlerComponent?> entity,
        bool refresh = true,
        bool autoStand = true,
        bool drop = true,
        bool force = false)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        return TryKnockdown(entity, entity.Comp.DefaultKnockedDuration, refresh, autoStand, drop, force);
    }

    /// <summary>
    ///     Checks if we can knock down an entity to the ground...
    /// </summary>
    /// <param name="entity">The entity we're trying to knock down</param>
    /// <param name="time">The time of the knockdown</param>
    /// <param name="autoStand">Whether we want to automatically stand when knockdown ends.</param>
    /// <param name="drop">Whether we should drop items.</param>
    /// <param name="force">Should we force the status effect?</param>
    public bool CanKnockdown(Entity<StandingStateComponent?> entity, ref TimeSpan? time, ref bool autoStand, ref bool drop, bool force = false)
    {
        if (time <= TimeSpan.Zero)
            return false;

        // Can't fall down if you can't actually be downed.
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        var evAttempt = new KnockDownAttemptEvent(autoStand, drop, time);
        RaiseLocalEvent(entity, ref evAttempt);

        autoStand = evAttempt.AutoStand;
        drop = evAttempt.Drop;

        return force || !evAttempt.Cancelled;
    }

    /// <summary>
    ///     Knocks down the entity, making it fall to the ground.
    /// </summary>
    /// <param name="entity">The entity we're trying to knock down</param>
    /// <param name="time">The time of the knockdown</param>
    /// <param name="refresh">Whether we should refresh a running timer or add to it, if one exists.</param>
    /// <param name="autoStand">Whether we want to automatically stand when knockdown ends.</param>
    /// <param name="drop">Whether we should drop items.</param>
    /// <param name="force">Should we force the status effect?</param>
    public bool TryKnockdown(Entity<CrawlerComponent?> entity, TimeSpan? time, bool refresh = true, bool autoStand = true, bool drop = true, bool force = false)
    {
        if (!CanKnockdown(entity.Owner, ref time, ref autoStand, ref drop, force))
            return false;

        // If the entity can't crawl they also need to be stunned, and therefore we should be using paralysis status effect.
        // Also time shouldn't be null if we're and trying to add time but, we check just in case anyways.
        if (!Resolve(entity, ref entity.Comp, false))
            return refresh || time == null ? TryUpdateParalyzeDuration(entity, time) : TryAddParalyzeDuration(entity, time.Value);

        Knockdown(entity, time, refresh, autoStand, drop);
        return true;
    }

    private void Crawl(Entity<CrawlerComponent?> entity, TimeSpan? time, bool refresh, bool autoStand, bool drop)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        Knockdown(entity, time, refresh, autoStand, drop);
    }

    private void Knockdown(EntityUid uid, TimeSpan? time, bool refresh, bool autoStand, bool drop)
    {
        // Initialize our component with the relevant data we need if we don't have it
        if (EnsureComp<KnockedDownComponent>(uid, out var component))
        {
            RefreshKnockedMovement((uid, component));
            CancelKnockdownDoAfter((uid, component));
        }
        else
        {
            // Only drop items the first time we want to fall...
            if (drop)
            {
                var ev = new DropHandItemsEvent();
                RaiseLocalEvent(uid, ref ev);
            }

            // Only update Autostand value if it's our first time being knocked down...
            SetAutoStand((uid, component), autoStand);
        }

        var knockedEv = new KnockedDownEvent();
        RaiseLocalEvent(uid, ref knockedEv);

        if (time != null)
        {
            UpdateKnockdownTime((uid, component), time.Value, refresh);
            _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} was knocked down for {time.Value.Seconds} seconds");
        }
        else
        {
            Alerts.ShowAlert(uid, KnockdownAlert);
            _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} was knocked down");
        }
    }

    public bool TryAddParalyzeDuration(EntityUid uid, TimeSpan? duration)
    {
        if (duration == null)
            return TryUpdateParalyzeDuration(uid, duration);

        if (!_status.TryAddStatusEffectDuration(uid, StunId, duration.Value))
            return false;

        // We can't exit knockdown when we're stunned, so this prevents knockdown lasting longer than the stun.
        Knockdown(uid, null, false, true, true);
        OnStunnedSuccessfully(uid, duration);

        return true;
    }

    public bool TryUpdateParalyzeDuration(EntityUid uid, TimeSpan? duration)
    {
        if (!_status.TryUpdateStatusEffectDuration(uid, StunId, duration))
            return false;

        // We can't exit knockdown when we're stunned, so this prevents knockdown lasting longer than the stun.
        Knockdown(uid, null, false, true, true);
        OnStunnedSuccessfully(uid, duration);

        return true;
    }

    public bool TryUnstun(Entity<StunnedComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return true;

        var ev = new StunEndAttemptEvent();
        RaiseLocalEvent(entity, ref ev);

        return !ev.Cancelled && RemComp<StunnedComponent>(entity);
    }

    private void OnStunStatusApplied(Entity<StunnedStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
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

    private void OnKnockdownStatusApplied(Entity<KnockdownStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        // If you make something that shouldn't crawl, crawl, that's your own fault.
        if (entity.Comp.Crawl)
            Crawl(args.Target, null, true, true, drop: entity.Comp.Drop);
        else
            Knockdown(args.Target, null, true, true, drop: entity.Comp.Drop);
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
