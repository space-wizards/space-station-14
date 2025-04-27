using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Input;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Slippery;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Stunnable;

public abstract class SharedStunSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    private readonly HashSet<EntityUid> _toUpdate = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<KnockedDownComponent, ComponentInit>(OnKnockInit);
        SubscribeLocalEvent<KnockedDownComponent, ComponentShutdown>(OnKnockShutdown);
        SubscribeLocalEvent<KnockedDownComponent, StandAttemptEvent>(OnStandAttempt);

        SubscribeLocalEvent<SlowedDownComponent, ComponentInit>(OnSlowInit);
        SubscribeLocalEvent<SlowedDownComponent, ComponentShutdown>(OnSlowRemove);

        SubscribeLocalEvent<StunnedComponent, ComponentStartup>(UpdateCanMove);
        SubscribeLocalEvent<StunnedComponent, ComponentShutdown>(UpdateCanMove);

        SubscribeLocalEvent<StunOnContactComponent, ComponentStartup>(OnStunOnContactStartup);
        SubscribeLocalEvent<StunOnContactComponent, StartCollideEvent>(OnStunOnContactCollide);

        // helping people up if they're knocked down
        SubscribeLocalEvent<KnockedDownComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<SlowedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<KnockedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshKnockedSpeed);

        SubscribeLocalEvent<KnockedDownComponent, RefreshFrictionModifiersEvent>(OnRefreshFriction);
        SubscribeLocalEvent<KnockedDownComponent, TileFrictionEvent>(OnKnockedTileFriction);

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
        SubscribeLocalEvent<KnockedDownComponent, BuckleAttemptEvent>(OnBuckleAttempt);
        SubscribeLocalEvent<MobStateComponent, MobStateChangedEvent>(OnMobStateChanged);

        // DoAfter event subscriptions
        SubscribeLocalEvent<KnockedDownComponent, TryStandDoAfterEvent>(OnStandDoAfter);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleKnockdown, InputCmdHandler.FromDelegate(HandleToggleKnockdown, handle: false))
            .Register<SharedStunSystem>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        foreach (var uid in _toUpdate)
        {
            if (HasComp<StunnedComponent>(uid)
                || !TryComp<KnockedDownComponent>(uid, out var knockedDown)
                || knockedDown.NextUpdate >= _gameTiming.CurTime)
                continue;

            // Try to stand, if we can stand, don't try to do it again
            if (!knockedDown.AutoStand || TryStanding(uid, knockedDown))
                _toUpdate.Remove(uid);
        }
    }

    private void OnAttemptInteract(Entity<StunnedComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMobStateChanged(EntityUid uid, MobStateComponent component, MobStateChangedEvent args)
    {
        if (!TryComp<StatusEffectsComponent>(uid, out var status))
        {
            return;
        }
        switch (args.NewMobState)
        {
            case MobState.Alive:
                {
                    break;
                }
            case MobState.Critical:
                {
                    _statusEffect.TryRemoveStatusEffect(uid, "Stun");
                    break;
                }
            case MobState.Dead:
                {
                    _statusEffect.TryRemoveStatusEffect(uid, "Stun");
                    break;
                }
            case MobState.Invalid:
            default:
                return;
        }

    }

    private void UpdateCanMove(EntityUid uid, StunnedComponent component, EntityEventArgs args)
    {
        _blocker.UpdateCanMove(uid);
    }

    private void OnStunOnContactStartup(Entity<StunOnContactComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<PhysicsComponent>(ent, out var body))
            _broadphase.RegenerateContacts((ent, body));
    }

    private void OnStunOnContactCollide(Entity<StunOnContactComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        if (_entityWhitelist.IsBlacklistPass(ent.Comp.Blacklist, args.OtherEntity))
            return;

        if (!TryComp<StatusEffectsComponent>(args.OtherEntity, out var status))
            return;

        TryStun(args.OtherEntity, ent.Comp.Duration, true, status);
        TryKnockdown(args.OtherEntity, ent.Comp.Duration, ent.Comp.Refresh, ent.Comp.AutoStand, status);
    }

    private void OnKnockInit(EntityUid uid, KnockedDownComponent component, ComponentInit args)
    {
        _standingState.Down(uid);
    }

    private void OnKnockShutdown(EntityUid uid, KnockedDownComponent component, ComponentShutdown args)
    {
        _toUpdate.Remove(uid);
        // This is jank but if we don't do this it'll still use the knockedDownComponent modifiers for friction because it hasn't been deleted quite yet.
        component.FrictionModifier = 1f;
        component.SpeedModifier = 1f;
        _standingState.Stand(uid);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        _movementSpeedModifier.RefreshFrictionModifiers(uid);
    }

    private void OnStandAttempt(EntityUid uid, KnockedDownComponent component, StandAttemptEvent args)
    {
        if (component.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    private void OnSlowInit(EntityUid uid, SlowedDownComponent component, ComponentInit args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnSlowRemove(EntityUid uid, SlowedDownComponent component, ComponentShutdown args)
    {
        component.SprintSpeedModifier = 1f;
        component.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    // TODO STUN: Make events for different things. (Getting modifiers, attempt events, informative events...)

    /// <summary>
    ///     Stuns the entity, disallowing it from doing many interactions temporarily.
    /// </summary>
    public bool TryStun(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!Resolve(uid, ref status, false))
            return false;

        if (!_statusEffect.TryAddStatusEffect<StunnedComponent>(uid, "Stun", time, refresh))
            return false;

        var ev = new StunnedEvent();
        RaiseLocalEvent(uid, ref ev);

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} stunned for {time.Seconds} seconds");
        return true;
    }

    /// <summary>
    ///     Knocks down the entity, making it fall to the ground.
    /// </summary>
    public bool TryKnockdown(EntityUid uid, TimeSpan time, bool refresh, bool autostand, StatusEffectsComponent? status = null)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!Resolve(uid, ref status, false))
            return false;

        if (!_statusEffect.CanApplyEffect(uid, "KnockedDown", status))
            return false;

        // Don't override our auto-stand preference if we're already knocked down, if we're trying to stand, stop that
        if (!EnsureComp<KnockedDownComponent>(uid, out var component))
            component.AutoStand = autostand;
        else if (component.DoAfter != null)
        {
            // This shit mispredicts like a motherfucker
            _doAfter.Cancel(component.DoAfter.Value);
            component.DoAfter = null;
        }

        var ev = new KnockedDownEvent()
        {
            KnockdownTime = time,
            AutoStand = autostand,
        };
        RaiseLocalEvent(uid, ref ev);

        if (component.AutoStand)
            _toUpdate.Add(uid);
        component.NextUpdate = _gameTiming.CurTime + time;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        _movementSpeedModifier.RefreshFrictionModifiers(uid);

        return true;
    }

    /// <summary>
    ///     Applies knockdown and stun to the entity temporarily.
    /// </summary>
    public bool TryParalyze(EntityUid uid, TimeSpan time, bool refresh,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        return TryKnockdown(uid, time, refresh, true, status) && TryStun(uid, time, refresh, status);
    }

    /// <summary>
    ///     Slows down the mob's walking/running speed temporarily
    /// </summary>
    public bool TrySlowdown(EntityUid uid, TimeSpan time, bool refresh,
        float walkSpeedMultiplier = 1f, float runSpeedMultiplier = 1f,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        if (time <= TimeSpan.Zero)
            return false;

        if (_statusEffect.TryAddStatusEffect<SlowedDownComponent>(uid, "SlowedDown", time, refresh, status))
        {
            var slowed = Comp<SlowedDownComponent>(uid);
            // Doesn't make much sense to have the "TrySlowdown" method speed up entities now does it?
            walkSpeedMultiplier = Math.Clamp(walkSpeedMultiplier, 0f, 1f);
            runSpeedMultiplier = Math.Clamp(runSpeedMultiplier, 0f, 1f);

            slowed.WalkSpeedModifier *= walkSpeedMultiplier;
            slowed.SprintSpeedModifier *= runSpeedMultiplier;

            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

            return true;
        }

        return false;
    }

    public bool TryStanding(EntityUid uid, KnockedDownComponent? comp)
    {
        // If we aren't knocked down or can't be knocked down, then we did technically succeed in standing up
        if (!Resolve(uid, ref comp, false) || !TryComp<StandingStateComponent>(uid, out var state))
            return true;

        if (comp.NextUpdate >= _gameTiming.CurTime)
            return false;

        // TODO: Check if we even have space to stand

        var ev = new StandupAttemptEvent()
        {
            DoAfterTime = state.StandTime,
            AutoStand = comp.AutoStand,
        };
        RaiseLocalEvent(uid, ref ev);

        if (ev.Cancelled)
            return false;

        var doAfterArgs = new DoAfterArgs(EntityManager, uid, ev.DoAfterTime, new TryStandDoAfterEvent(), uid, uid)
        {
            BreakOnDamage = true,
            DamageThreshold = 5,
            CancelDuplicate = true,
            RequireCanInteract = false
        };

        // If we try standing don't try standing again
        _toUpdate.Remove(uid);
        if (!_doAfter.TryStartDoAfter(doAfterArgs, out var id))
            return false;

        // Store the DoAfter id so we can cancel it if needed
        comp.DoAfter = id;
        return true;
    }

    private void OnStandDoAfter(Entity<KnockedDownComponent> entity, ref TryStandDoAfterEvent args)
    {
        entity.Comp.DoAfter = null;

        if (args.Cancelled)
            return;

        RemComp<KnockedDownComponent>(entity);
    }

    private void HandleToggleKnockdown(ICommonSession? session)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } playerEnt || !Exists(playerEnt))
            return;

        if (!TryComp<KnockedDownComponent>(playerEnt, out var component))
            TryKnockdown(playerEnt, TimeSpan.FromSeconds(0.5), false, false);
        else
            component.AutoStand = !TryStanding(playerEnt, component);
    }

    private void OnInteractHand(EntityUid uid, KnockedDownComponent knocked, InteractHandEvent args)
    {
        if (args.Handled || knocked.HelpTimer > 0f)
            return;

        // TODO: This should be an event.
        if (HasComp<SleepingComponent>(uid))
            return;

        // Set it to half the help interval so helping is actually useful...
        knocked.HelpTimer = knocked.HelpInterval / 2f;

        _statusEffect.TryRemoveTime(uid, "KnockedDown", TimeSpan.FromSeconds(knocked.HelpInterval));
        _audio.PlayPredicted(knocked.StunAttemptSound, uid, args.User);
        Dirty(uid, knocked);

        args.Handled = true;
    }

    #region friction and movement listeners

    private void OnKnockedTileFriction(Entity<KnockedDownComponent> entity, ref TileFrictionEvent args)
    {
        args.Modifier *= entity.Comp.FrictionModifier;
    }

    private void OnRefreshFriction(Entity<KnockedDownComponent> entity, ref RefreshFrictionModifiersEvent args)
    {
        args.ModifyFriction(entity.Comp.FrictionModifier);
        args.ModifyAcceleration(entity.Comp.FrictionModifier);
    }

    private void OnRefreshMovespeed(EntityUid ent, SlowedDownComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(comp.WalkSpeedModifier, comp.SprintSpeedModifier);
    }

    private void OnRefreshKnockedSpeed(EntityUid ent, KnockedDownComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(comp.SpeedModifier);
    }

    #endregion

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

    private void OnBuckleAttempt(Entity<KnockedDownComponent> ent, ref BuckleAttemptEvent args)
    {
        if (args.User == ent && ent.Comp.NextUpdate > _gameTiming.CurTime)
            args.Cancelled = true;
    }

    #endregion
}

/// <summary>
///     Raised directed on an entity when it is stunned.
/// </summary>
[ByRefEvent]
public record struct StunnedEvent;

/// <summary>
///     Raised directed on an entity when it is knocked down.
/// </summary>
[ByRefEvent]
public record struct KnockedDownEvent
{
    public TimeSpan KnockdownTime;
    public bool AutoStand;
    public float SpeedModifier;
    public float FrictionModifier;
}

/// <summary>
///     Raised directed on an entity when it tries to stand up
/// </summary>
[ByRefEvent]
public record struct StandupAttemptEvent(bool Cancelled)
{
    public bool AutoStand;
    public TimeSpan DoAfterTime;
}

[Serializable, NetSerializable]
public sealed partial class TryStandDoAfterEvent : SimpleDoAfterEvent;
