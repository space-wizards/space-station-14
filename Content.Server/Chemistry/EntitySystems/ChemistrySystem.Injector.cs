using System;
using System.Threading;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.CombatMode;
using Content.Server.DoAfter;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem
{
    private void InitializeInjector()
    {
        SubscribeLocalEvent<InjectorComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<InjectorComponent, HandDeselectedEvent>(OnInjectorDeselected);
        SubscribeLocalEvent<InjectorComponent, ComponentStartup>(OnInjectorStartup);
        SubscribeLocalEvent<InjectorComponent, UseInHandEvent>(OnInjectorUse);
        SubscribeLocalEvent<InjectorComponent, AfterInteractEvent>(OnInjectorAfterInteract);
        SubscribeLocalEvent<InjectorComponent, ComponentGetState>(OnInjectorGetState);

        SubscribeLocalEvent<InjectionCompleteEvent>(OnInjectionComplete);
        SubscribeLocalEvent<InjectionCancelledEvent>(OnInjectionCancelled);
    }

    private static void OnInjectionCancelled(InjectionCancelledEvent ev)
    {
        ev.Component.CancelToken = null;
    }

    private void OnInjectionComplete(InjectionCompleteEvent ev)
    {
        ev.Component.CancelToken = null;
        UseInjector(ev.Target, ev.User, ev.Component);
    }

    private void UseInjector(EntityUid target, EntityUid user, InjectorComponent component)
    { 
        // Handle injecting/drawing for solutions
        if (component.ToggleState == SharedInjectorComponent.InjectorToggleMode.Inject)
        {
            if (_solutions.TryGetInjectableSolution(target, out var injectableSolution))
            {
                TryInject(component, target, injectableSolution, user, false);
            }
            else if (_solutions.TryGetRefillableSolution(target, out var refillableSolution))
            {
                TryInject(component, target, refillableSolution, user, true);
            }
            else if (TryComp<BloodstreamComponent>(target, out var bloodstream))
            {
                TryInjectIntoBloodstream(component, bloodstream, user);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("injector-component-cannot-transfer-message",
                    ("target", target)), component.Owner, Filter.Entities(user));
            }
        }
        else if (component.ToggleState == SharedInjectorComponent.InjectorToggleMode.Draw)
        {
            if (_solutions.TryGetDrawableSolution(target, out var drawableSolution))
            {
                TryDraw(component, target, drawableSolution, user);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("injector-component-cannot-draw-message",
                    ("target", target)), component.Owner, Filter.Entities(user));
            }
        }
    }

    private static void OnInjectorDeselected(EntityUid uid, InjectorComponent component, HandDeselectedEvent args)
    {
        component.CancelToken?.Cancel();
        component.CancelToken = null;
    }

    private void OnSolutionChange(EntityUid uid, InjectorComponent component, SolutionChangedEvent args)
    {
        Dirty(component);
    }

    private void OnInjectorGetState(EntityUid uid, InjectorComponent component, ref ComponentGetState args)
    {
        _solutions.TryGetSolution(uid, InjectorComponent.SolutionName, out var solution);

        var currentVolume = solution?.CurrentVolume ?? FixedPoint2.Zero;
        var maxVolume = solution?.MaxVolume ?? FixedPoint2.Zero;

        args.State = new SharedInjectorComponent.InjectorComponentState(currentVolume, maxVolume, component.ToggleState);
    }

    private void OnInjectorAfterInteract(EntityUid uid, InjectorComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach) return;

        if (component.CancelToken != null)
        {
            component.CancelToken.Cancel();
            component.CancelToken = null;
            args.Handled = true;
            return;
        }

        //Make sure we have the attacking entity
        if (args.Target is not { Valid: true } target ||
            !HasComp<SolutionContainerManagerComponent>(uid))
        {
            return;
        }

        // Is the target a mob? If yes, use a do-after to give them time to respond.
        if (HasComp<MobStateComponent>(target) ||
            HasComp<BloodstreamComponent>(target))
        {
            InjectDoAfter(component, args.User, target);
            args.Handled = true;
            return;
        }

        UseInjector(target, args.User, component);
        args.Handled = true;
    }

    private void OnInjectorStartup(EntityUid uid, InjectorComponent component, ComponentStartup args)
    {
        Dirty(component);
    }

    private void OnInjectorUse(EntityUid uid, InjectorComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        Toggle(component, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Toggle between draw/inject state if applicable
    /// </summary>
    private void Toggle(InjectorComponent component, EntityUid user)
    {
        if (component.InjectOnly)
        {
            return;
        }

        string msg;
        switch (component.ToggleState)
        {
            case SharedInjectorComponent.InjectorToggleMode.Inject:
                component.ToggleState = SharedInjectorComponent.InjectorToggleMode.Draw;
                msg = "injector-component-drawing-text";
                break;
            case SharedInjectorComponent.InjectorToggleMode.Draw:
                component.ToggleState = SharedInjectorComponent.InjectorToggleMode.Inject;
                msg = "injector-component-injecting-text";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _popup.PopupEntity(Loc.GetString(msg), component.Owner, Filter.Entities(user));
    }

    /// <summary>
    /// Send informative pop-up messages and wait for a do-after to complete.
    /// </summary>
    private void InjectDoAfter(InjectorComponent component, EntityUid user, EntityUid target)
    {
        // Create a pop-up for the user
        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, Filter.Entities(user));

        if (!_solutions.TryGetSolution(component.Owner, InjectorComponent.SolutionName, out var solution))
            return;

        var actualDelay = MathF.Max(component.Delay, 1f);
        if (user != target)
        {
            // Create a pop-up for the target
            var userName = MetaData(user).EntityName;
            _popup.PopupEntity(Loc.GetString("injector-component-injecting-target",
                ("user", userName)), user, Filter.Entities(target));

            // Check if the target is incapacitated or in combat mode and modify time accordingly.
            if (TryComp<MobStateComponent>(target, out var mobState) && mobState.IsIncapacitated())
            {
                actualDelay /= 2;
            }
            else if (TryComp<CombatModeComponent>(target, out var combat) && combat.IsInCombatMode)
            {
                // Slightly increase the delay when the target is in combat mode. Helps prevents cheese injections in
                // combat with fast syringes & lag.
                actualDelay += 1;
            }

            // Add an admin log, using the "force feed" log type. It's not quite feeding, but the effect is the same.
            if (component.ToggleState == SharedInjectorComponent.InjectorToggleMode.Inject)
            {
                _logs.Add(LogType.ForceFeed,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to inject {EntityManager.ToPrettyString(target):target} with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}");
            }
        }
        else
        {
            // Self-injections take half as long.
            actualDelay /= 2;

            if (component.ToggleState == SharedInjectorComponent.InjectorToggleMode.Inject)
                _logs.Add(LogType.Ingestion,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to inject themselves with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}.");
        }

        component.CancelToken = new CancellationTokenSource();

        _doAfter.DoAfter(new DoAfterEventArgs(user, actualDelay, component.CancelToken.Token, target)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            MovementThreshold = 0.1f,
            BroadcastFinishedEvent = new InjectionCompleteEvent()
            {
                Component = component,
                User = user,
                Target = target,
            },
            BroadcastCancelledEvent = new InjectionCancelledEvent()
            {
                Component = component,
            }
        });
    }

    private void TryInjectIntoBloodstream(InjectorComponent component, BloodstreamComponent targetBloodstream, EntityUid user)
    {
        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetBloodstream.Solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-cannot-inject-message", ("target", targetBloodstream.Owner)),
                component.Owner, Filter.Entities(user));
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutions.SplitSolution(user, targetBloodstream.Solution, realTransferAmount);

        _blood.TryAddToBloodstream((targetBloodstream).Owner, removedSolution, targetBloodstream);

        removedSolution.DoEntityReaction(targetBloodstream.Owner, ReactionMethod.Injection);

        _popup.PopupEntity(Loc.GetString("injector-component-inject-success-message",
                ("amount", removedSolution.TotalVolume),
                ("target", targetBloodstream.Owner)), component.Owner, Filter.Entities(user));

        Dirty(component);
        AfterInject(component);
    }

    private void TryInject(InjectorComponent component, EntityUid targetEntity, Solution targetSolution, EntityUid user, bool asRefill)
    {
        if (!_solutions.TryGetSolution(component.Owner, InjectorComponent.SolutionName, out var solution)
            || solution.CurrentVolume == 0)
        {
            return;
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetSolution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-target-already-full-message", ("target", targetEntity)),
                component.Owner, Filter.Entities(user));
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutions.SplitSolution(component.Owner, solution, realTransferAmount);

        removedSolution.DoEntityReaction(targetEntity, ReactionMethod.Injection);

        if (!asRefill)
        {
            _solutions.Inject(targetEntity, targetSolution, removedSolution);
        }
        else
        {
            _solutions.Refill(targetEntity, targetSolution, removedSolution);
        }

        _popup.PopupEntity(Loc.GetString("injector-component-transfer-success-message",
                ("amount", removedSolution.TotalVolume),
                ("target", targetEntity)), component.Owner, Filter.Entities(user));

        Dirty(component);
        AfterInject(component);
    }

    private void AfterInject(InjectorComponent component)
    {
        // Automatically set syringe to draw after completely draining it.
        if (_solutions.TryGetSolution(component.Owner, InjectorComponent.SolutionName, out var solution)
            && solution.CurrentVolume == 0)
        {
            component.ToggleState = SharedInjectorComponent.InjectorToggleMode.Draw;
        }
    }

    private void AfterDraw(InjectorComponent component)
    {
        // Automatically set syringe to inject after completely filling it.
        if (_solutions.TryGetSolution(component.Owner, InjectorComponent.SolutionName, out var solution)
            && solution.AvailableVolume == 0)
        {
            component.ToggleState = SharedInjectorComponent.InjectorToggleMode.Inject;
        }
    }

    private void TryDraw(InjectorComponent component, EntityUid targetEntity, Solution targetSolution, EntityUid user)
    {
        if (!_solutions.TryGetSolution(component.Owner, InjectorComponent.SolutionName, out var solution)
            || solution.AvailableVolume == 0)
        {
            return;
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetSolution.DrawAvailable);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-target-is-empty-message", ("target", targetEntity)),
                component.Owner, Filter.Entities(user));
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutions.Draw(targetEntity, targetSolution, realTransferAmount);

        if (!_solutions.TryAddSolution(component.Owner, solution, removedSolution))
        {
            return;
        }

        _popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
                ("amount", removedSolution.TotalVolume),
                ("target", targetEntity)), component.Owner, Filter.Entities(user));

        Dirty(component);
        AfterDraw(component);
    }

    private sealed class InjectionCompleteEvent : EntityEventArgs
    {
        public InjectorComponent Component { get; init; } = default!;
        public EntityUid User { get; init; }
        public EntityUid Target { get; init; }
    }

    private sealed class InjectionCancelledEvent : EntityEventArgs
    {
        public InjectorComponent Component { get; init; } = default!;
    }
}
