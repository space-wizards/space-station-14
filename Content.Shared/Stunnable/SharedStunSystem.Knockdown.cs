using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable;

/// <summary>
/// This contains the knockdown logic for the stun system for organization purposes.
/// </summary>
public abstract partial class SharedStunSystem
{
    /// <inheritdoc/>
    public void InitializeKnockdown()
    {
        SubscribeLocalEvent<KnockedDownComponent, RejuvenateEvent>(OnRejuvenate);

        // Startup and Shutdown
        SubscribeLocalEvent<KnockedDownComponent, ComponentInit>(OnKnockInit);
        SubscribeLocalEvent<KnockedDownComponent, ComponentShutdown>(OnKnockShutdown);

        // Action blockers
        SubscribeLocalEvent<KnockedDownComponent, BuckleAttemptEvent>(OnBuckleAttempt);
        SubscribeLocalEvent<KnockedDownComponent, StandAttemptEvent>(OnStandAttempt);

        // Helping people stand up
        SubscribeLocalEvent<KnockedDownComponent, InteractHandEvent>(OnInteractHand);

        // Updating movement a friction
        SubscribeLocalEvent<KnockedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshKnockedSpeed);
        SubscribeLocalEvent<KnockedDownComponent, RefreshFrictionModifiersEvent>(OnRefreshFriction);
        SubscribeLocalEvent<KnockedDownComponent, TileFrictionEvent>(OnKnockedTileFriction);
        SubscribeLocalEvent<KnockedDownComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<KnockedDownComponent, DidUnequipHandEvent>(OnHandUnequipped);

        // DoAfter event subscriptions
        SubscribeLocalEvent<KnockedDownComponent, TryStandDoAfterEvent>(OnStandDoAfter);
        SubscribeLocalEvent<KnockedDownComponent, KnockedDownEvent>(OnSubsequentKnockdown);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleKnockdown, InputCmdHandler.FromDelegate(HandleToggleKnockdown, handle: false))
            .Register<SharedStunSystem>();
    }

    private void OnRejuvenate(Entity<KnockedDownComponent> entity, ref RejuvenateEvent args)
    {
        entity.Comp.NextUpdate = _gameTiming.CurTime;

        if (entity.Comp.AutoStand)
            RemComp<KnockedDownComponent>(entity);
    }

    #region Startup and Shutdown

    private void OnKnockInit(Entity<KnockedDownComponent> ent, ref ComponentInit args)
    {
        _standingState.Down(ent, true, ent.Comp.AutoStand);
    }

    private void OnKnockShutdown(Entity<KnockedDownComponent> ent, ref ComponentShutdown args)
    {
        // This is jank but if we don't do this it'll still use the knockedDownComponent modifiers for friction because it hasn't been deleted quite yet.
        ent.Comp.FrictionModifier = 1f;
        ent.Comp.SpeedModifier = 1f;

        _standingState.Stand(ent);

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
        _movementSpeedModifier.RefreshFrictionModifiers(ent);
    }

    #endregion

    #region Action Blockers

    private void OnStandAttempt(Entity<KnockedDownComponent> ent, ref StandAttemptEvent args)
    {
        if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    private void OnBuckleAttempt(Entity<KnockedDownComponent> ent, ref BuckleAttemptEvent args)
    {
        if (args.User == ent && ent.Comp.NextUpdate > _gameTiming.CurTime)
            args.Cancelled = true;
    }

    #endregion

    #region Knockdown Logic

    private void HandleToggleKnockdown(ICommonSession? session)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } playerEnt || !Exists(playerEnt))
            return;

        if (!TryComp<KnockedDownComponent>(playerEnt, out var component))
            TryKnockdown(playerEnt, TimeSpan.FromSeconds(0.5), false, false);
        else
            component.AutoStand = TryStanding(playerEnt, out component.DoAfter); // Have a better way of doing this
    }

    public bool TryStanding(Entity<KnockedDownComponent?> ent, out DoAfterId? id)
    {
        id = null;
        // If we aren't knocked down or can't be knocked down, then we did technically succeed in standing up
        if (!Resolve(ent, ref ent.Comp, false) || !TryComp<StandingStateComponent>(ent, out var state))
            return true;

        if (!_blocker.CanMove(ent))
            return false; // We should also remove from update until we can move again.

        if (ent.Comp.NextUpdate >= _gameTiming.CurTime)
            return false;

        // TODO: Check if we even have space to stand

        var ev = new StandupAttemptEvent()
        {
            DoAfterTime = state.StandTime,
            AutoStand = ent.Comp.AutoStand,
        };
        RaiseLocalEvent(ent, ref ev);

        if (ev.Cancelled)
            return false;

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, ev.DoAfterTime, new TryStandDoAfterEvent(), ent, ent)
        {
            BreakOnDamage = true,
            DamageThreshold = 5,
            CancelDuplicate = true,
            RequireCanInteract = false,
            BreakOnHandChange = true
        };

        // If we try standing don't try standing again
        return _doAfter.TryStartDoAfter(doAfterArgs, out id);
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

    #endregion

    #region DoAfter

    private void OnStandDoAfter(Entity<KnockedDownComponent> entity, ref TryStandDoAfterEvent args)
    {
        entity.Comp.DoAfter = null;

        if (args.Cancelled || !HasComp<KnockedDownComponent>(entity))
            return;

        RemComp<KnockedDownComponent>(entity);
    }

    private void OnSubsequentKnockdown(Entity<KnockedDownComponent> ent, ref KnockedDownEvent args)
    {
        if (!ent.Comp.DoAfter.HasValue)
            return;

        _doAfter.Cancel(ent.Comp.DoAfter.Value);
        ent.Comp.DoAfter = null;
    }

    #endregion

    #region Movement and Friction

    private void RefreshKnockedMovement(EntityUid uid, KnockedDownComponent component, StandingStateComponent? standing = null)
    {
        if (!Resolve(uid, ref standing, false))
            return;

        var ev = new KnockedDownRefreshEvent()
        {
            SpeedModifier = standing.SpeedModifier,
            FrictionModifier = standing.FrictionModifier,
        };
        RaiseLocalEvent(uid, ref ev);

        component.SpeedModifier = ev.SpeedModifier;
        component.FrictionModifier = ev.FrictionModifier;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        _movementSpeedModifier.RefreshFrictionModifiers(uid);
    }

    private void OnRefreshKnockedSpeed(EntityUid ent, KnockedDownComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(comp.SpeedModifier);
    }

    private void OnKnockedTileFriction(Entity<KnockedDownComponent> entity, ref TileFrictionEvent args)
    {
        args.Modifier *= entity.Comp.FrictionModifier;
    }

    private void OnRefreshFriction(Entity<KnockedDownComponent> entity, ref RefreshFrictionModifiersEvent args)
    {
        args.ModifyFriction(entity.Comp.FrictionModifier);
        args.ModifyAcceleration(entity.Comp.FrictionModifier);
    }

    private void OnHandEquipped(Entity<KnockedDownComponent> ent, ref DidEquipHandEvent args)
    {
        RefreshKnockedMovement(ent, ent.Comp);
    }

    private void OnHandUnequipped(Entity<KnockedDownComponent> ent, ref DidUnequipHandEvent args)
    {
        RefreshKnockedMovement(ent, ent.Comp);
    }

    #endregion

    #region Events

    /// <summary>
    ///     Raised directed on an entity when it is knocked down.
    /// </summary>
    [ByRefEvent]
    public record struct KnockDownAttemptEvent(bool Cancelled = false)
    {
        public bool AutoStand;
    }

    /// <summary>
    ///     Raised directed on an entity when it is knocked down.
    /// </summary>
    [ByRefEvent]
    public record struct KnockedDownEvent
    {
        public TimeSpan KnockdownTime;
    }

    /// <summary>
    ///     Raised on an entity that needs to refresh its knockdown modifiers
    /// </summary>
    [ByRefEvent]
    public record struct KnockedDownRefreshEvent
    {
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

    [ByRefEvent, Serializable, NetSerializable]
    public sealed partial class TryStandDoAfterEvent : SimpleDoAfterEvent;


    #endregion
}
