using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Hypospray.Events;
using Content.Shared.CombatMode;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed class HypospraySystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainers = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HyposprayComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HyposprayComponent, MeleeHitEvent>(OnAttack);
        SubscribeLocalEvent<HyposprayComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HyposprayComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleModeVerb);
        SubscribeLocalEvent<HyposprayComponent, HyposprayDoAfterEvent>(OnHypoInjectDoAfter);
    }

    #region Ref events
    private void OnUseInHand(Entity<HyposprayComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryDoInject(entity, args.User, args.User);
    }

    private void OnAfterInteract(Entity<HyposprayComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        args.Handled = TryUseHypospray(entity, args.Target.Value, args.User);
    }

    private void OnAttack(Entity<HyposprayComponent> entity, ref MeleeHitEvent args)
    {
        if (args.HitEntities is [])
            return;

        TryDoInject(entity, args.HitEntities[0], args.User);
    }

    #endregion

    #region Draw/Inject
    private bool TryUseHypospray(Entity<HyposprayComponent> entity, EntityUid target, EntityUid user)
    {
        // if target is ineligible but is a container, try to draw from the container if allowed
        if (entity.Comp.CanContainerDraw
            && !EligibleEntity(target, entity)
            && _solutionContainers.TryGetDrawableSolution(target, out var drawableSolution, out _))
        {
            return TryDraw(entity, target, drawableSolution.Value, user);
        }
        var component = entity.Comp;
        var injectTime = component.InjectTime;
        if (injectTime == TimeSpan.Zero || !HasComp<MobStateComponent>(target))
            return TryDoInject(entity, target, user);

        injectTime = GetInjectTime(entity, user, target);
        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, injectTime, new HyposprayDoAfterEvent(),entity, target: target, used: entity)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = component.NeedHand,
            BreakOnHandChange = component.BreakOnHandChange,
            MovementThreshold = component.MovementThreshold,
        });
    }

    private bool TryDoInject(Entity<HyposprayComponent> entity, EntityUid target, EntityUid user)
    {
        var (uid, component) = entity;

        if (!EligibleEntity(target, component))
            return false;

        if (TryComp(uid, out UseDelayComponent? delayComp))
        {
            if (_useDelay.IsDelayed((uid, delayComp)))
                return false;
        }

        string? msgFormat = null;

        // Self-event
        var selfEvent = new SelfBeforeHyposprayInjectsEvent(user, entity.Owner, target);
        RaiseLocalEvent(user, selfEvent);

        if (selfEvent.Cancelled)
        {
            _popup.PopupClient(Loc.GetString(selfEvent.InjectMessageOverride ?? "hypospray-cant-inject", ("owner", Identity.Entity(target, EntityManager))), target, user);
            return false;
        }

        target = selfEvent.TargetGettingInjected;

        if (!EligibleEntity(target, component))
            return false;

        // Target event
        var targetEvent = new TargetBeforeHyposprayInjectsEvent(user, entity.Owner, target);
        RaiseLocalEvent(target, targetEvent);

        if (targetEvent.Cancelled)
        {
            _popup.PopupClient(Loc.GetString(targetEvent.InjectMessageOverride ?? "hypospray-cant-inject", ("owner", Identity.Entity(target, EntityManager))), target, user);
            return false;
        }

        target = targetEvent.TargetGettingInjected;

        if (!EligibleEntity(target, component))
            return false;

        // The target event gets priority for the overriden message.
        if (targetEvent.InjectMessageOverride != null)
            msgFormat = targetEvent.InjectMessageOverride;
        else if (selfEvent.InjectMessageOverride != null)
            msgFormat = selfEvent.InjectMessageOverride;
        else if (target == user)
            msgFormat = "hypospray-component-inject-self-message";

        if (!_solutionContainers.TryGetSolution(uid, component.SolutionName, out var hypoSpraySoln, out var hypoSpraySolution) || hypoSpraySolution.Volume == 0)
        {
            _popup.PopupClient(Loc.GetString("hypospray-component-empty-message"), target, user);
            return true;
        }

        if (!_solutionContainers.TryGetInjectableSolution(target, out var targetSoln, out var targetSolution))
        {
            _popup.PopupClient(Loc.GetString("hypospray-cant-inject", ("target", Identity.Entity(target, EntityManager))), target, user);
            return false;
        }

        _popup.PopupClient(Loc.GetString(msgFormat ?? "hypospray-component-inject-other-message", ("other", Identity.Entity(target, EntityManager))), target, user);

        if (target != user)
        {
            _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);
            // TODO: This should just be using melee attacks...
            // meleeSys.SendLunge(angle, user);
        }

        // Get transfer amount. May be smaller than component.TransferAmount if not enough room
        // Additionally, if it has InjectMaxCapacity on true, attempt to inject entire capacity.
        var realTransferAmount = FixedPoint2.Min(component.InjectMaxCapacity ? hypoSpraySolution.Volume : component.TransferAmount, targetSolution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupClient(Loc.GetString("hypospray-component-transfer-already-full-message", ("owner", target)), target, user);
            return true;
        }

        _audio.PlayPredicted(component.InjectSound, target, user);

        // Medipens and such use this system and don't have a delay, requiring extra checks
        // BeginDelay function returns if item is already on delay
        if (delayComp != null)
            _useDelay.TryResetDelay((uid, delayComp));

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutionContainers.SplitSolution(hypoSpraySoln.Value, realTransferAmount);

        if (!targetSolution.CanAddSolution(removedSolution))
            return true;
        _reactiveSystem.DoEntityReaction(target, removedSolution, ReactionMethod.Injection);
        _solutionContainers.TryAddSolution(targetSoln.Value, removedSolution);

        var ev = new TransferDnaEvent { Donor = target, Recipient = uid };
        RaiseLocalEvent(target, ref ev);

        // same LogType as syringes...
        _adminLogger.Add(LogType.ForceFeed, $"{ToPrettyString(user):user} injected {ToPrettyString(target):target} with a solution {SharedSolutionContainerSystem.ToPrettyString(removedSolution):removedSolution} using a {ToPrettyString(uid):using}");
        return true;
    }

    private bool TryDraw(Entity<HyposprayComponent> entity, EntityUid target, Entity<SolutionComponent> targetSolution, EntityUid user)
    {
        if (!_solutionContainers.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out var soln,
                out var solution) || solution.AvailableVolume == 0)
        {
            return false;
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room, also make sure there's room in the injector
        var realTransferAmount = FixedPoint2.Min(entity.Comp.TransferAmount, targetSolution.Comp.Solution.Volume,
            solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupClient(
                Loc.GetString("injector-component-target-is-empty-message",
                    ("target", Identity.Entity(target, EntityManager))),
                entity.Owner, user);
            return false;
        }

        var removedSolution = _solutionContainers.Draw(target, targetSolution, realTransferAmount);

        if (!_solutionContainers.TryAddSolution(soln.Value, removedSolution))
        {
            return false;
        }

        _popup.PopupClient(Loc.GetString("injector-component-draw-success-message",
            ("amount", removedSolution.Volume),
            ("target", Identity.Entity(target, EntityManager))), entity.Owner, user);
        return true;
    }

    private bool EligibleEntity(EntityUid entity, HyposprayComponent component)
    {
        // TODO: Does checking for BodyComponent make sense as a "can be hypospray'd" tag?
        // In SS13 the hypospray ONLY works on mobs, NOT beakers or anything else.
        // But this is 14, we dont do what SS13 does just because SS13 does it.
        return component.OnlyAffectsMobs
            ? HasComp<SolutionContainerManagerComponent>(entity) &&
              HasComp<MobStateComponent>(entity)
            : HasComp<SolutionContainerManagerComponent>(entity);
    }

    /// <summary>
    /// Calculate the Injection time for non-instant hyposprays.
    /// It also sends informative pop-up messages specific to non-instant hyposprays.
    /// </summary>
    private TimeSpan GetInjectTime(Entity<HyposprayComponent> hypospray, EntityUid user, EntityUid target)
    {
        // Create a pop-up for the user
        _popup.PopupClient(Loc.GetString("hypospray-component-injecting-user"), target, user);
        var hypoComp = hypospray.Comp;

        if (!_solutionContainers.TryGetSolution(hypospray.Owner, hypoComp.SolutionName, out var _,
                out var solution))
        {
            return TimeSpan.Zero;
        }

        var actualDelay = hypoComp.Delay;
        FixedPoint2 amountToInject;
        // If the Hypospray injects it's entire capacity, set the amount to that.
        if (hypoComp.InjectMaxCapacity)
        {
            amountToInject = solution.Volume;
        }
        else
        {
            // additional delay is based on actual volume left to inject in hypospray when smaller than transfer amount
            amountToInject = FixedPoint2.Min(hypoComp.TransferAmount, solution.Volume);
        }

        // Non-instant hypospray Injections take 0.25 seconds longer per 5u of possible space/content
        // First 5u(MinimumTransferAmount) doesn't incur delay
        actualDelay += hypoComp.DelayPerVolume * FixedPoint2.Max(0, amountToInject - 5).Double();

        // Ensure that the minimum delay before incapacitation checks is 1 seconds
        actualDelay = MathHelper.Max(actualDelay, TimeSpan.FromSeconds(1));

        if (user != target) // injecting someone else
        {
            // Create a pop-up for the target
            var userName = Identity.Entity(user, EntityManager);
            _popup.PopupEntity(Loc.GetString("hypospray-component-injecting-target",
                ("user", userName)), user, target);

            // Check if the target is incapacitated or in combat mode and modify time accordingly.
            if (_mobState.IsIncapacitated(target))
            {
                actualDelay /= 2.5f;
            }
            else if (_combatMode.IsInCombatMode(target))
            {
                // Slightly increase the delay when the target is in combat mode. Helps prevents cheese injections in
                // combat with fast syringes & lag.
                actualDelay += TimeSpan.FromSeconds(1);
            }

            // Add an admin log, using the "force feed" log type. It's not quite feeding, but the effect is the same.
            _adminLogger.Add(LogType.ForceFeed,
                $"{ToPrettyString(user):user} is attempting to inject {ToPrettyString(target):target} with a solution {SharedSolutionContainerSystem.ToPrettyString(solution):solution}");
        }
        else // injecting yourself
        {
            // Self-injections take half as long.
            actualDelay /= 2;

            _adminLogger.Add(LogType.Ingestion,
                $"{ToPrettyString(user):user} is attempting to inject themselves with a solution {SharedSolutionContainerSystem.ToPrettyString(solution):solution}.");

        }

        return actualDelay;
    }

    /// <summary>
    /// Handles the doAfter for non-instant hyposprays.
    /// </summary>
    private void OnHypoInjectDoAfter(Entity<HyposprayComponent> ent, ref HyposprayDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        args.Handled = TryDoInject(ent, args.Target.Value, args.User);
    }
    #endregion

    #region Verbs

    // <summary>
    // Uses the OnlyMobs field as a check to implement the ability
    // to draw from jugs and containers with the hypospray
    // Toggleable to allow people to inject containers if they prefer it over drawing
    // </summary>
    private void AddToggleModeVerb(Entity<HyposprayComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || entity.Comp.InjectOnly)
            return;

        var user = args.User;
        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("hypospray-verb-mode-label"),
            Act = () =>
            {
                ToggleMode(entity, user);
            }
        };
        args.Verbs.Add(verb);
    }

    private void ToggleMode(Entity<HyposprayComponent> entity, EntityUid user)
    {
        SetMode(entity, !entity.Comp.OnlyAffectsMobs);
        var msg = (entity.Comp.OnlyAffectsMobs && entity.Comp.CanContainerDraw) ? "hypospray-verb-mode-inject-mobs-only" : "hypospray-verb-mode-inject-all";
        _popup.PopupClient(Loc.GetString(msg), entity, user);
    }

    public void SetMode(Entity<HyposprayComponent> entity, bool onlyAffectsMobs)
    {
        if (entity.Comp.OnlyAffectsMobs == onlyAffectsMobs)
            return;

        entity.Comp.OnlyAffectsMobs = onlyAffectsMobs;
        Dirty(entity);
    }

    #endregion
}
