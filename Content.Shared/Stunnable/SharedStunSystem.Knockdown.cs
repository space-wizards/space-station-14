using Content.Shared.Alert;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Robust.Shared.Audio;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable;

/// <summary>
/// This contains the knockdown logic for the stun system for organization purposes.
/// </summary>
public abstract partial class SharedStunSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    private void InitializeKnockdown()
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

        // Handling Alternative Inputs
        SubscribeAllEvent<ForceStandUpEvent>(OnForceStandup);
        SubscribeLocalEvent<KnockedDownComponent, KnockedDownAlertEvent>(OnKnockedDownAlert);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleKnockdown, InputCmdHandler.FromDelegate(HandleToggleKnockdown, handle: false))
            .Register<SharedStunSystem>();
    }

    private void OnRejuvenate(Entity<KnockedDownComponent> entity, ref RejuvenateEvent args)
    {
        entity.Comp.NextUpdate = GameTiming.CurTime;

        if (entity.Comp.AutoStand)
            RemComp<KnockedDownComponent>(entity);
    }

    #region Startup and Shutdown

    private void OnKnockInit(Entity<KnockedDownComponent> ent, ref ComponentInit args)
    {
        _standingState.Down(ent, true, ent.Comp.AutoStand);
        RefreshKnockedMovement(ent);
    }

    private void OnKnockShutdown(Entity<KnockedDownComponent> ent, ref ComponentShutdown args)
    {
        // This is jank but if we don't do this it'll still use the knockedDownComponent modifiers for friction because it hasn't been deleted quite yet.
        ent.Comp.FrictionModifier = 1f;
        ent.Comp.SpeedModifier = 1f;

        _standingState.Stand(ent);
        Alerts.ClearAlert(ent, "Knockdown");

        var ev = new KnockdownEndEvent();
        RaiseLocalEvent(ent, ref ev);

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
        _movementSpeedModifier.RefreshFrictionModifiers(ent);
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
            TryKnockdown(playerEnt, TimeSpan.FromSeconds(0.5), false, false); // TODO: Unhardcode these numbers
        else
            component.AutoStand = TryStanding(playerEnt, out component.DoAfter); // Have a better way of doing this
    }

    public bool TryStanding(Entity<KnockedDownComponent?> ent, out DoAfterId? id, bool failIfBreak = false)
    {
        id = null;
        // If we aren't knocked down or can't be knocked down, then we did technically succeed in standing up
        if (!Resolve(ent, ref ent.Comp, false) || !TryComp<StandingStateComponent>(ent, out var state))
            return true;

        if (ent.Comp.NextUpdate >= GameTiming.CurTime)
            return false;

        if (failIfBreak && ent.Comp.DoAfter.HasValue)
        {
            // If we would break an existing do after, return false
            id = ent.Comp.DoAfter.Value;
            return false;
        }

        if (!CanStand(ent))
            return false;

        var ev = new StandUpArgsEvent()
        {
            DoAfterTime = state.StandTime,
            AutoStand = ent.Comp.AutoStand,
        };
        RaiseLocalEvent(ent, ref ev);

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, ev.DoAfterTime, new TryStandDoAfterEvent(), ent, ent)
        {
            BreakOnDamage = true,
            DamageThreshold = 5,
            CancelDuplicate = true,
            RequireCanInteract = false,
            BreakOnHandChange = true
        };

        // If we try standing don't try standing again
        return DoAfter.TryStartDoAfter(doAfterArgs, out id);
    }

    private bool CanStand(EntityUid entity)
    {
        // TODO: For testing, having a collider block you from standing doesn't stop the attempt, just makes the do-after fail
        // Commented out for testing
        //if (StandingBlocked(entity))
            //return false;

        // Added for testing
        if (!_blocker.CanMove(entity))
            return false;

        var ev = new StandUpAttemptEvent();
        RaiseLocalEvent(entity, ref ev);

        return !ev.Cancelled;
    }

    private bool StandingBlocked(EntityUid uid)
    {
        if (!_blocker.CanMove(uid))
            return true;

        // Check if we would intersect with any entities by standing up
        if (GetStandingColliders(uid))
        {
            _popup.PopupClient(Loc.GetString("knockdown-component-stand-no-room"), uid, uid, PopupType.SmallCaution);
            return true;
        }

        return false;
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

    private void OnForceStandup(ForceStandUpEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        if (TryComp<KnockedDownComponent>(user, out var component))
            ForceStandUp((user, component));
    }

    public void ForceStandUp(Entity<KnockedDownComponent> ent)
    {
        // That way if we fail to stand, the game will try to stand for us when we are able to
        ent.Comp.AutoStand = true;

        if (!TryComp<StandingStateComponent>(ent, out var stand) || StandingBlocked(ent))
            return;

        // TODO: This is an extension of the test, if we go back to the old system remove this event and change StandingBlocked to !CanStand
        var ev = new StandUpAttemptEvent();
        RaiseLocalEvent(ent, ref ev);

        if (ev.Cancelled)
            return;

        if (!TryComp<StaminaComponent>(ent, out var stamina))
            return;

        if (!_hands.TryCountEmptyHands(ent, out var hands) || hands <= 0)
            return;

        var staminaDamage = stamina.ForceStandStamina;
        var timeLeft = Math.Max(0, (float)(ent.Comp.NextUpdate - GameTiming.CurTime).TotalSeconds);

        // Add more stamina damage so that way we can't stand up for 0 stamina cost
        staminaDamage += staminaDamage * timeLeft / (float)hands; // TODO: Unhardcode this and make it part of an event
        // Average them
        staminaDamage /= 2;

        // TODO: Raise an event to modify the stamina damage?

        if (!Stamina.TryTakeStamina(ent, staminaDamage, stamina, visual: true))
        {
            _popup.PopupClient(Loc.GetString("knockdown-component-pushup-failure"), ent, ent, PopupType.MediumCaution);
            return;
        }

        // If we have a DoAfter, cancel it
        DoAfter.Cancel(ent.Comp.DoAfter);
        // Remove Component
        RemComp<KnockedDownComponent>(ent);

        _popup.PopupClient(Loc.GetString("knockdown-component-pushup-success"), ent, ent);
        _audio.PlayPredicted(stamina.ForceStandSuccessSound, ent.Owner, ent.Owner, AudioParams.Default.WithVariation(0.025f).WithVolume(5f));

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(ent):user} has force stood up from knockdown.");
    }

    private void OnKnockedDownAlert(Entity<KnockedDownComponent> ent, ref KnockedDownAlertEvent args)
    {
        if (args.Handled)
            return;

        // If we're already trying to stand, or we fail to stand try forcing it
        if (!TryStanding(ent.Owner, out ent.Comp.DoAfter, true))
            ForceStandUp(ent);

        args.Handled = true;
    }

    /// <summary>
    ///     Checks if standing would cause us to collide with something and potentially get stuck.
    ///     Returns true if we will collide with something, and false if we will not.
    /// </summary>
    private bool GetStandingColliders(Entity<TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var intersecting = _physics.GetEntitiesIntersectingBody(entity, 4, false);

        if (intersecting.Count == 0)
            return false;

        var fixtureQuery = GetEntityQuery<FixturesComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        var ourAABB = _entityLookup.GetAABBNoContainer(entity, entity.Comp.LocalPosition, entity.Comp.LocalRotation);

        foreach (var ent in intersecting)
        {
            if (!fixtureQuery.TryGetComponent(ent, out var fixtures))
                continue;

            if (!xformQuery.TryComp(ent, out var xformComp))
                continue;

            var xform = new Transform(xformComp.LocalPosition, xformComp.LocalRotation);

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (!fixture.Hard || (fixture.CollisionMask & 4) != 4)
                    continue;

                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    var intersection = fixture.Shape.ComputeAABB(xform, i).IntersectPercentage(ourAABB);
                    if (intersection > 0.1f)
                        return true;
                }
            }
        }

        return false;
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
        if (args.User == ent && ent.Comp.NextUpdate > GameTiming.CurTime)
            args.Cancelled = true;
    }

    #endregion

    #region DoAfter

    private void OnStandDoAfter(Entity<KnockedDownComponent> entity, ref TryStandDoAfterEvent args)
    {
        entity.Comp.DoAfter = null;

        if (args.Cancelled)
        {
            if (entity.Comp.AutoStand)
                entity.Comp.NextUpdate = GameTiming.CurTime + TimeSpan.FromSeconds(0.5f); // TODO: Unhardcode this
            return;
        }

        if (StandingBlocked(entity))
            return;

        RemComp<KnockedDownComponent>(entity);

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(entity):user} has stood up from knockdown.");
    }

    // TODO: This shit does not predict on the client at all
    private void OnSubsequentKnockdown(Entity<KnockedDownComponent> ent, ref KnockedDownEvent args)
    {
        if (!ent.Comp.DoAfter.HasValue)
            return;

        DoAfter.Cancel(ent.Comp.DoAfter.Value);
        ent.Comp.DoAfter = null;
    }

    #endregion

    #region Movement and Friction

    private void RefreshKnockedMovement(Entity<KnockedDownComponent> ent, StandingStateComponent? standing = null)
    {
        if (!Resolve(ent, ref standing, false))
            return;

        var ev = new KnockedDownRefreshEvent()
        {
            SpeedModifier = standing.SpeedModifier,
            FrictionModifier = standing.FrictionModifier,
        };
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.SpeedModifier = ev.SpeedModifier;
        ent.Comp.FrictionModifier = ev.FrictionModifier;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
        _movementSpeedModifier.RefreshFrictionModifiers(ent);
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
        RefreshKnockedMovement(ent);
    }

    private void OnHandUnequipped(Entity<KnockedDownComponent> ent, ref DidUnequipHandEvent args)
    {
        RefreshKnockedMovement(ent);
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
    public record struct StandUpAttemptEvent(bool Cancelled);

    [ByRefEvent]
    public record struct StandUpArgsEvent
    {
        public bool AutoStand;
        public TimeSpan DoAfterTime;
    }

    [ByRefEvent]

    public record struct KnockdownEndEvent;

    /// <summary>
    ///     Raised when you click on the Knocked Down Alert
    /// </summary>
    public sealed partial class KnockedDownAlertEvent : BaseAlertEvent;

    [ByRefEvent, Serializable, NetSerializable]
    public sealed partial class TryStandDoAfterEvent : SimpleDoAfterEvent;

    [Serializable, NetSerializable]
    public sealed class ForceStandUpEvent : EntityEventArgs;

    #endregion
}
