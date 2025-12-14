using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Events;
using Content.Shared.CombatMode;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed partial class InjectorSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedForensicsSystem _forensics = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InjectorComponent, UseInHandEvent>(OnInjectorUse);
        SubscribeLocalEvent<InjectorComponent, AfterInteractEvent>(OnInjectorAfterInteract);
        SubscribeLocalEvent<InjectorComponent, InjectorDoAfterEvent>(OnInjectDoAfter);
        SubscribeLocalEvent<InjectorComponent, MeleeHitEvent>(OnAttack);
        SubscribeLocalEvent<InjectorComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
    }

    #region Events Handling
    private void OnInjectorUse(Entity<InjectorComponent> injector, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (injector.Comp.TransferAmounts.Count <= 1) // Injectors that can't toggle transferAmounts will be used.
            MobsDoAfter(injector, args.User, args.User);
        else // Syringes toggle Draw/Inject.
            Toggle(injector, args.User);

        args.Handled = true;
    }

    private void OnInjectorAfterInteract(Entity<InjectorComponent> injector, ref AfterInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || args.Target is not { Valid: true } target
            || !HasComp<SolutionContainerManagerComponent>(injector))
            return;

        // Is the target a mob? If yes, use a do-after to give them time to respond.
        if (HasComp<MobStateComponent>(target) || HasComp<BloodstreamComponent>(target))
        {
            // Are use using an injector capable of targeting a mob?
            if (injector.Comp.IgnoreMobs)
                return;

            MobsDoAfter(injector, args.User, target);
            args.Handled = true;
            return;
        }

        // Draw from or inject into jugs, bottles, etc.
        ContainerDoAfter(injector, args.User, target);
        args.Handled = true;
    }

    private void OnInjectDoAfter(Entity<InjectorComponent> injector, ref InjectorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        args.Handled = TryUseInjector(injector, args.Args.User, args.Args.Target.Value);
    }

    private void OnAttack(Entity<InjectorComponent> injector, ref MeleeHitEvent args)
    {
        if (args.HitEntities is [])
            return;

        MobsDoAfter(injector, args.User, args.HitEntities[0]);
    }

    private void AddSetTransferVerbs(Entity<InjectorComponent> injector, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        AlternativeVerb? dynamicVerb = null;

        // Allow switching between Dynamic and Inject mode to inject into containers.
        if (injector.Comp.AllowedModes.HasFlag(InjectorToggleMode.Dynamic | InjectorToggleMode.Inject))
        {
            dynamicVerb = new AlternativeVerb
            {
                Text = Loc.GetString("injector-toggle-verb-text"),
                Act = () =>
                {
                    ToggleDynamic(injector, user);
                },
            };
        }

        // If currentTransferAmount is null, this injector injects all its contents upon usage.
        // Therefore, it mustn't change its transferAmount. Otherwise, check if it can even cycle.
        if (injector.Comp.CurrentTransferAmount == null || injector.Comp.TransferAmounts is not { Count: > 1 })
        {
            if (dynamicVerb != null)
                args.Verbs.Add(dynamicVerb);
            return;
        }

        var min = injector.Comp.TransferAmounts.Min();
        var max = injector.Comp.TransferAmounts.Max();
        var cur = injector.Comp.CurrentTransferAmount;
        var toggleAmount = cur == max ? min : max;

        var priority = 0;
        AlternativeVerb toggleVerb = new()
        {
            Text = Loc.GetString("comp-solution-transfer-verb-toggle", ("amount", toggleAmount)),
            Category = VerbCategory.SetTransferAmount,
            Act = () =>
            {
                injector.Comp.CurrentTransferAmount = toggleAmount;
                _popup.PopupClient(Loc.GetString("comp-solution-transfer-set-amount", ("amount", toggleAmount)), user, user);
                Dirty(injector);
            },

            Priority = priority
        };
        args.Verbs.Add(toggleVerb);

        priority -= 1;

        // Add specific transfer verbs for amounts defined in the component
        foreach (var amount in injector.Comp.TransferAmounts)
        {
            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount)),
                Category = VerbCategory.SetTransferAmount,
                Act = () =>
                {
                    injector.Comp.CurrentTransferAmount = amount;
                    _popup.PopupClient(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)), user, user);
                    Dirty(injector);
                },

                // we want to sort by size, not alphabetically by the verb text.
                Priority = priority
            };

            priority -= 1;

            args.Verbs.Add(verb);
        }

        if (dynamicVerb == null)
            return;
        // Add Dynamic verb at last, so it doesn't interfere with volume toggling.
        dynamicVerb.Priority = priority;
        args.Verbs.Add(dynamicVerb);
    }
    #endregion Events Handling

    #region Mob Interaction
    /// <summary>
    /// Send informative pop-up messages and wait for a do-after to complete.
    /// </summary>
    private void MobsDoAfter(Entity<InjectorComponent> injector, EntityUid user, EntityUid target)
    {
        if (_useDelay.IsDelayed(injector.Owner) // Check for Delay.
            || !GetMobsDoAfterTime(injector, user, target, out var doAfterTime)) // Get the DoAfter time.
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, doAfterTime, new InjectorDoAfterEvent(), injector.Owner, target: target, used: injector.Owner)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = injector.Comp.NeedHand,
            BreakOnHandChange = injector.Comp.BreakOnHandChange,
            MovementThreshold = injector.Comp.MovementThreshold,
        });

        // If the DoAfter was instant, don't send popups and logs indicating an attempt.
        if (doAfterTime == TimeSpan.Zero)
            return;

        if (!_solutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution, out var injectorSolution))
            return;

        // Create a pop-up for the user
        _popup.PopupClient(injector.Comp.ToggleState == InjectorToggleMode.Draw
                ? Loc.GetString("injector-component-drawing-user")
                : Loc.GetString(injector.Comp.PreparingInjectorUser),
            target,
            user);

        if (user != target) // injecting someone else
        {
            // Create a pop-up for the target
            var userName = Identity.Entity(user, EntityManager);
            if (injector.Comp.ToggleState == InjectorToggleMode.Draw)
            {
                _popup.PopupEntity(Loc.GetString("injector-component-drawing-target",
                    ("user", userName)), user, target);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString(injector.Comp.PreparingInjectorTarget,
                    ("user", userName)), user, target);
            }

            // Add an admin log, using the "force-feed" log type. It's not quite feeding, but the effect is the same.
            if (injector.Comp.ToggleState == InjectorToggleMode.Draw)
            {
                _adminLogger.Add(LogType.ForceFeed,
                    $"{ToPrettyString(user):user} is attempting to draw {injector.Comp.CurrentTransferAmount.ToString()} units from {ToPrettyString(target):target}");
            }
            else
            {
                _adminLogger.Add(LogType.ForceFeed,
                    $"{ToPrettyString(user):user} is attempting to inject {ToPrettyString(target):target} with a solution {SharedSolutionContainerSystem.ToPrettyString(injectorSolution):solution}");
            }
        }
        else // injecting yourself
        {
            if (injector.Comp.ToggleState == InjectorToggleMode.Draw)
            {
                _adminLogger.Add(LogType.ForceFeed,
                    $"{ToPrettyString(user):user} is attempting to draw {injector.Comp.CurrentTransferAmount.ToString()} units from themselves.");
            }
            else
            {
                _adminLogger.Add(LogType.Ingestion,
                    $"{ToPrettyString(user):user} is attempting to inject themselves with a solution {SharedSolutionContainerSystem.ToPrettyString(injectorSolution):solution}.");
            }
        }
    }

    /// <summary>
    /// Get the DoAfter Time for Containers.
    /// </summary>
    /// <param name="injector">The injector that is interacting with the mob.</param>
    /// <param name="user">The user using the injector.</param>
    /// <param name="target">The target mob.</param>
    /// <param name="doAfterTime">The duration of the resulting doAfter.</param>
    /// <returns></returns>
    private bool GetMobsDoAfterTime(Entity<InjectorComponent> injector, EntityUid user, EntityUid target, out TimeSpan doAfterTime)
    {
        // If it's injecting and injection delays are zero, return zero.
        // Otherwise, it'll increase to the minimum of 1s, plus 1s if the target is in combat mode.
        if (injector.Comp.ToggleState.HasFlag(InjectorToggleMode.Dynamic & InjectorToggleMode.Inject)
            && injector.Comp.InjectTime == TimeSpan.Zero
            && injector.Comp.DelayPerVolume == TimeSpan.Zero)
        {
            doAfterTime = TimeSpan.Zero;
            return true;
        }

        doAfterTime = injector.Comp.InjectTime;

        if (!_solutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution, out var injectorSolution))
            return false;

        FixedPoint2 amountToInject;
        if (injector.Comp.ToggleState == InjectorToggleMode.Draw && injector.Comp.CurrentTransferAmount != null)
        {
            // additional delay is based on actual volume left to draw in syringe when smaller than transfer amount
            amountToInject = FixedPoint2.Min(injector.Comp.CurrentTransferAmount.Value, injectorSolution.AvailableVolume);
        }
        else
        {
            // additional delay is based on actual volume left to inject in syringe when smaller than transfer amount
            // If CurrentTransferAmount is null, it'll want to inject its entire contents, e.g., epipens.
            var plannedAmount = injector.Comp.CurrentTransferAmount ?? injectorSolution.Volume;
            amountToInject = FixedPoint2.Min(plannedAmount, injectorSolution.Volume);
        }

        // Injections over the IgnoreDelayForVolume amount take Xu times DelayPerVolume longer.
        doAfterTime += injector.Comp.DelayPerVolume * FixedPoint2.Max(0, amountToInject - injector.Comp.IgnoreDelayForVolume).Double();

        // Ensure that the minimum delay before incapacitation checks is 1 seconds
        doAfterTime = MathHelper.Max(doAfterTime, TimeSpan.FromSeconds(1));

        if (user != target) // injecting someone else
        {
            // Check if the target is incapacitated or in combat mode and modify time accordingly.
            if (_mobState.IsIncapacitated(target))
            {
                doAfterTime /= 2.5f;
            }
            else if (_combatMode.IsInCombatMode(target))
            {
                // Slightly increase the delay when the target is in combat mode. Helps prevent cheese injections in
                // combat with fast syringes and lag.
                doAfterTime += TimeSpan.FromSeconds(1);
            }
        }
        else // injecting yourself
        {
            // Self-injections take half as long.
            doAfterTime /= 2;
        }

        return true;
    }
    #endregion Mob Interaction

    #region Container Interaction
    private void ContainerDoAfter(Entity<InjectorComponent> injector, EntityUid user, EntityUid target)
    {
        if (!GetContainerDoAfterTime(injector, user, target, out var doAfterTime))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, doAfterTime, new InjectorDoAfterEvent(), injector.Owner, target: target, used: injector.Owner)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = injector.Comp.NeedHand,
            BreakOnHandChange = injector.Comp.BreakOnHandChange,
            MovementThreshold = injector.Comp.MovementThreshold,
        });
    }

    /// <summary>
    /// Get the DoAfter Time for Containers.
    /// </summary>
    /// <param name="injector">The injector that is interacting with the container.</param>
    /// <param name="user">The user using the injector.</param>
    /// <param name="target">The target container,</param>
    /// <param name="doAfterTime">The duration of the resulting DoAfter.</param>
    /// <returns></returns>
    private bool GetContainerDoAfterTime(Entity<InjectorComponent> injector, EntityUid user, EntityUid target, out TimeSpan doAfterTime)
    {
        doAfterTime = TimeSpan.Zero;

        if (!injector.Comp.ToggleState.HasAnyFlag(InjectorToggleMode.Draw | InjectorToggleMode.Dynamic))
            return true;

        if (!_solutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution, out var solution)
            || solution.AvailableVolume == 0)
        {
            _popup.PopupClient(Loc.GetString("injector-component-cannot-toggle-draw-message"), user, user);
            return false; // If already full, fail drawing.
        }

        if (!_solutionContainer.TryGetDrawableSolution(target, out _, out var drawableSol))
        {
            _popup.PopupClient(Loc.GetString("injector-component-cannot-transfer-message", ("target", Identity.Entity(target, EntityManager))), injector, user);
            return false;
        }

        if (drawableSol.Volume == 0)
        {
            _popup.PopupClient(Loc.GetString("injector-component-target-is-empty-message", ("target", Identity.Entity(target, EntityManager))), injector, user);
            return false;
        }

        doAfterTime = injector.Comp.DrawTime; // Check if the Injector has a draw time, but only when drawing.

        return true;
    }
    #endregion Container Interaction

    #region Injecting/Drawing
    private bool TryUseInjector(Entity<InjectorComponent> injector, EntityUid user, EntityUid target)
    {
        var isOpenOrIgnored = injector.Comp.IgnoreClosed || !_openable.IsClosed(target);

        LocId msg = target == user ? "injector-component-cannot-transfer-message-self" : "injector-component-cannot-transfer-message";

        switch (injector.Comp.ToggleState)
        {
            // Handle injecting/drawing for solutions
            case InjectorToggleMode.Inject:
            {
                if (isOpenOrIgnored && _solutionContainer.TryGetInjectableSolution(target, out var injectableSolution, out _))
                    return TryInject(injector, user, target, injectableSolution.Value, false);

                if (isOpenOrIgnored && _solutionContainer.TryGetRefillableSolution(target, out var refillableSolution, out _))
                    return TryInject(injector, user, target, refillableSolution.Value, true);
                break;
            }
            case InjectorToggleMode.Draw:
            {
                // Draw from a bloodstream if the target has that
                if (TryComp<BloodstreamComponent>(target, out var stream) &&
                    _solutionContainer.ResolveSolution(target, stream.BloodSolutionName, ref stream.BloodSolution))
                {
                    return TryDraw(injector, user, (target, stream), stream.BloodSolution.Value);
                }

                // Draw from an object (food, beaker, etc)
                if (isOpenOrIgnored && _solutionContainer.TryGetDrawableSolution(target, out var drawableSolution, out _))
                    return TryDraw(injector, user, target, drawableSolution.Value);

                msg = target == user ? "injector-component-cannot-draw-message-self" : "injector-component-cannot-draw-message";
                _popup.PopupClient(Loc.GetString(msg, ("target", Identity.Entity(target, EntityManager))), injector, user);
                break;
            }
            case InjectorToggleMode.Dynamic:
            {
                if (HasComp<BloodstreamComponent>(target) // If it's a mob, inject. We're using injectableSolution so I don't have to code a sole method for injecting into bloodstreams.
                    && _solutionContainer.TryGetInjectableSolution(target, out var injectableSolution, out _))
                {
                    return TryInject(injector, user, target, injectableSolution.Value, false);
                }

                // Draw from an object (food, beaker, etc)
                if (isOpenOrIgnored && _solutionContainer.TryGetDrawableSolution(target, out var drawableSolution, out _))
                    return TryDraw(injector, user, target, drawableSolution.Value);
                break;
            }
        }

        _popup.PopupClient(Loc.GetString(msg, ("target", Identity.Entity(target, EntityManager))), injector, user);
        return false;
    }

    private bool TryInject(Entity<InjectorComponent> injector, EntityUid user, EntityUid target,
        Entity<SolutionComponent> targetSolution, bool asRefill)
    {
        if (!_solutionContainer.ResolveSolution(injector.Owner,
                injector.Comp.SolutionName,
                ref injector.Comp.Solution,
                out var injectorSolution) || injectorSolution.Volume == 0)
        { // If empty, show a popup.
            _popup.PopupClient(Loc.GetString("injector-component-empty-message", ("injector", injector)), user, user);
            return false;
        }

        var selfEv = new SelfBeforeInjectEvent(user, injector, target);
        RaiseLocalEvent(user, selfEv);

        if (selfEv.Cancelled)
        { // Clowns will now also fumble Syringes.
            if (selfEv.OverrideMessage != null)
                _popup.PopupPredicted(selfEv.OverrideMessage, user, user);
            return true;
        }

        target = selfEv.TargetGettingInjected;

        var ev = new TargetBeforeInjectEvent(user, injector, target, null);
        RaiseLocalEvent(target, ref ev);

        if (ev.Cancelled)
        { // Jugsuit blocking Hyposprays when
            var userMessage = Loc.GetString("injector-component-blocked-user");
            var otherMessage = Loc.GetString("injector-component-blocked-other", ("target", target), ("user", user));
            _popup.PopupPredicted(userMessage, otherMessage, target, user, PopupType.SmallCaution);
            return true;
        }

        // Get transfer amount. It may be smaller than _transferAmount if not enough room
        var plannedTransferAmount = FixedPoint2.Min(injector.Comp.CurrentTransferAmount ?? injectorSolution.Volume, injectorSolution.Volume);
        var realTransferAmount = FixedPoint2.Min(plannedTransferAmount, targetSolution.Comp.Solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            LocId msg = target == user ? "injector-component-target-already-full-message-self" : "injector-component-target-already-full-message";
            _popup.PopupClient(
                Loc.GetString(msg,
                    ("target", Identity.Entity(target, EntityManager))),
                injector.Owner,
                user);
            return false;
        }

        // Move units from attackSolution to targetSolution
        Solution removedSolution;
        if (TryComp<StackComponent>(target, out var stack))
            removedSolution = _solutionContainer.SplitStackSolution(injector.Comp.Solution.Value, realTransferAmount, stack.Count);
        else
            removedSolution = _solutionContainer.SplitSolution(injector.Comp.Solution.Value, realTransferAmount);

        _reactiveSystem.DoEntityReaction(target, removedSolution, ReactionMethod.Injection);

        if (!asRefill)
            _solutionContainer.Inject(target, targetSolution, removedSolution);
        else
            _solutionContainer.Refill(target, targetSolution, removedSolution);

        LocId msgSuccess = target == user ? "injector-component-transfer-success-message-self" : "injector-component-transfer-success-message";

        if (selfEv.OverrideMessage != null)
            msgSuccess = selfEv.OverrideMessage;
        else if (ev.OverrideMessage != null)
            msgSuccess = ev.OverrideMessage;

        _popup.PopupClient(
            Loc.GetString(msgSuccess,
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(target, EntityManager))),
            target, user);

        // it is IMPERATIVE that when an injector is instant, that it has a pop-up.
        if (injector.Comp.InjectPopupTarget != null && target != user)
            _popup.PopupClient(Loc.GetString(injector.Comp.InjectPopupTarget), target, target);

        // Some injectors like hyposprays have sound, some like syringes have not.
        if (injector.Comp.InjectSound != null)
            _audio.PlayPredicted(injector.Comp.InjectSound, injector, user);
        // Log what happened.
        _adminLogger.Add(LogType.ForceFeed, $"{ToPrettyString(user):user} injected {ToPrettyString(target):target} with a solution {SharedSolutionContainerSystem.ToPrettyString(removedSolution):removedSolution} using a {ToPrettyString(injector):using}");

        AfterInject(injector, target);
        return true;
    }

    private bool TryDraw(Entity<InjectorComponent> injector, EntityUid user, Entity<BloodstreamComponent?> target,
        Entity<SolutionComponent> targetSolution)
    {
        if (!_solutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution,
                out var solution) || solution.AvailableVolume == 0)
        {
            _popup.PopupClient("injector-component-cannot-toggle-draw-message", user, user);
            return false;
        }

        var applicableTargetSolution = targetSolution.Comp.Solution;
        // If a whitelist exists, remove all non-whitelisted reagents from the target solution temporarily
        var temporarilyRemovedSolution = new Solution();
        if (injector.Comp.ReagentWhitelist is { } reagentWhitelist)
        {
            temporarilyRemovedSolution = applicableTargetSolution.SplitSolutionWithout(applicableTargetSolution.Volume, reagentWhitelist.ToArray());
        }

        // If transferAmount is null, fallback to 5 units.
        var plannedTransferAmount = injector.Comp.CurrentTransferAmount ?? FixedPoint2.New(5);
        // Get transfer amount. It may be smaller than _transferAmount if not enough room, also make sure there's room in the injector
        var realTransferAmount = FixedPoint2.Min(plannedTransferAmount,
            applicableTargetSolution.Volume,
            solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            LocId msg = target.Owner == user ? "injector-component-target-is-empty-message-self" : "injector-component-target-is-empty-message";
            _popup.PopupClient(
                Loc.GetString(msg,
                    ("target", Identity.Entity(target, EntityManager))),
                injector.Owner, user);
            return false;
        }

        // We have some snowflaked behavior for streams.
        if (target.Comp != null)
        {
            DrawFromBlood(injector, (target.Owner, target.Comp), injector.Comp.Solution.Value, realTransferAmount, user);
            return true;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutionContainer.Draw(target.Owner, targetSolution, realTransferAmount);

        // Add back non-whitelisted reagents to the target solution
        _solutionContainer.TryAddSolution(targetSolution, temporarilyRemovedSolution);

        if (!_solutionContainer.TryAddSolution(injector.Comp.Solution.Value, removedSolution))
        {
            return false;
        }

        LocId msgSuccess = target.Owner == user ? "injector-component-draw-success-message-self" : "injector-component-draw-success-message";
        _popup.PopupClient(
            Loc.GetString(msgSuccess,
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(target, EntityManager))),
            injector.Owner, user);

        AfterDraw(injector, target);
        return true;
    }

    private void DrawFromBlood(Entity<InjectorComponent> injector, Entity<BloodstreamComponent> target,
        Entity<SolutionComponent> injectorSolution, FixedPoint2 transferAmount, EntityUid user)
    {
        var drawAmount = (float)transferAmount;

        if (_solutionContainer.ResolveSolution(target.Owner, target.Comp.ChemicalSolutionName,
                ref target.Comp.ChemicalSolution))
        {
            var chemTemp = _solutionContainer.SplitSolution(target.Comp.ChemicalSolution.Value, drawAmount * 0.15f);
            _solutionContainer.TryAddSolution(injectorSolution, chemTemp);
            drawAmount -= (float)chemTemp.Volume;
        }

        if (_solutionContainer.ResolveSolution(target.Owner, target.Comp.BloodSolutionName,
                ref target.Comp.BloodSolution))
        {
            var bloodTemp = _solutionContainer.SplitSolution(target.Comp.BloodSolution.Value, drawAmount);
            _solutionContainer.TryAddSolution(injectorSolution, bloodTemp);
        }

        LocId msg = target.Owner == user ? "injector-component-draw-success-message-self" : "injector-component-draw-success-message";
        _popup.PopupClient(
            Loc.GetString(msg,
                ("amount", transferAmount),
                ("target", Identity.Entity(target, EntityManager))),
            injector.Owner, user);

        AfterDraw(injector, target);
    }

    private void AfterInject(Entity<InjectorComponent> injector, EntityUid target)
    {
        // Leave some DNA from the injectee on it
        _forensics.TransferDna(injector, target);
        // Reset the delay, if present.
        if (TryComp<UseDelayComponent>(injector, out var delay))
            _useDelay.TryResetDelay((injector, delay));
        // Dynamic does not automatically set its mode to draw when empty, as it can still draw from containers.
        if (injector.Comp.ToggleState == InjectorToggleMode.Dynamic)
            return;

        // Automatically set syringe to draw after completely draining it.
        if (_solutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution,
                out var solution) && solution.Volume == 0)
        {
            SetMode(injector, InjectorToggleMode.Draw);
        }
    }

    private void AfterDraw(Entity<InjectorComponent> injector, EntityUid target)
    {
        // Automatically set the injector to inject after completely filling it.
        if (_solutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution,
                out var solution) && solution.AvailableVolume == 0
            && !injector.Comp.ToggleState.HasFlag(InjectorToggleMode.Dynamic)) // Unless it's in dynamic mode, as it can still inject.
        {
            SetMode(injector, InjectorToggleMode.Inject);
        }

        // Leave some DNA from the drawee on it
        _forensics.TransferDna(injector, target);
    }
    #endregion Injecting/Drawing

    #region Mode Toggling
    /// <summary>
    /// Toggle the injector between draw/inject state if applicable.
    /// </summary>
    public void Toggle(Entity<InjectorComponent> injector, EntityUid user)
    {
        // Check if the injector can only inject and skip if so. Otherwise, medipens will show weird popups.
        if (!injector.Comp.AllowedModes.HasAnyFlag(InjectorToggleMode.Draw | InjectorToggleMode.Dynamic)
            || !_solutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution, out var solution))
            return;

        string msg;

        switch (injector.Comp.ToggleState)
        {
            case InjectorToggleMode.Inject:
                if (solution.AvailableVolume > 0) // If a solution has empty space to fill up, allow toggling to draw
                {
                    SetMode(injector, InjectorToggleMode.Draw);
                    msg = "injector-component-drawing-text";
                }
                else
                {
                    msg = "injector-component-cannot-toggle-draw-message";
                }
                break;
            case InjectorToggleMode.Draw:
                if (solution.Volume > 0) // If a solution has anything in it, allow toggling to inject
                {
                    SetMode(injector, InjectorToggleMode.Inject);
                    msg = "injector-component-injecting-text";
                }
                else
                {
                    msg = "injector-component-cannot-toggle-inject-message";
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _popup.PopupClient(Loc.GetString(msg), injector, user);
    }

    /// <summary>
    /// Toggle the injector between dynamic/inject state if applicable.
    /// </summary>
    public void ToggleDynamic(Entity<InjectorComponent> injector, EntityUid user)
    {
        // Needs both modes, or else it cannot toggle.
        if (!injector.Comp.AllowedModes.HasFlag(InjectorToggleMode.Inject | InjectorToggleMode.Dynamic))
            return;

        string msg;

        switch (injector.Comp.ToggleState)
        {
            case InjectorToggleMode.Inject:
                // Sets it to dynamic mode.
                SetMode(injector, InjectorToggleMode.Dynamic);
                msg = "injector-component-dynamic-text";
                break;
            case InjectorToggleMode.Draw:
                // Sets it to dynamic mode.
                SetMode(injector, InjectorToggleMode.Dynamic);
                msg = "injector-component-dynamic-text";
                break;
            case InjectorToggleMode.Dynamic:
                // Sets it to inject so the injector can inject into containers.
                SetMode(injector, InjectorToggleMode.Inject);
                msg = "injector-component-injecting-text";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _popup.PopupClient(Loc.GetString(msg), injector, user);
    }

    /// <summary>
    /// Set the mode of the injector to draw or inject.
    /// </summary>
    private void SetMode(Entity<InjectorComponent> injector, InjectorToggleMode mode)
    {
        if (!injector.Comp.AllowedModes.HasFlag(mode)) // Check if they can access the mode.
            return;

        injector.Comp.ToggleState = mode;
        Dirty(injector);
    }
    #endregion Mode Toggling
}
