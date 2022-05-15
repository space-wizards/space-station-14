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
using Content.Shared.Interaction.Events;
using Content.Shared.MobState.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem
{
    private void InitializeIVBag()
    {
        SubscribeLocalEvent<IVBagComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<IVBagComponent, HandDeselectedEvent>(OnInjectorDeselected);
        SubscribeLocalEvent<IVBagComponent, ComponentStartup>(OnInjectorStartup);
        SubscribeLocalEvent<IVBagComponent, UseInHandEvent>(OnInjectorUse);
        SubscribeLocalEvent<IVBagComponent, AfterInteractEvent>(OnInjectorAfterInteract);
        SubscribeLocalEvent<IVBagComponent, ComponentGetState>(OnInjectorGetState);

        SubscribeLocalEvent<BagInjectionCompleteEvent>(OnBagInjectionComplete);
        SubscribeLocalEvent<BagInjectionCancelledEvent>(OnBagInjectionCancelled);
    }

    private static void OnBagInjectionCancelled(BagInjectionCancelledEvent ev)
    {
        ev.Component.CancelToken = null;
    }

    private void OnBagInjectionComplete(BagInjectionCompleteEvent ev)
    {
        ev.Component.CancelToken = null;
        UseInjector(ev.Target, ev.User, ev.Component);
    }

    private void UseInjector(EntityUid target, EntityUid user, IVBagComponent component)
    {
        // Handle injecting/drawing for solutions
        if (component.ToggleState == SharedIVBagComponent.IVBagToggleMode.Inject)
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
        else if (component.ToggleState == SharedIVBagComponent.IVBagToggleMode.Draw)
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

    private static void OnInjectorDeselected(EntityUid uid, IVBagComponent component, HandDeselectedEvent args)
    {
        component.CancelToken?.Cancel();
        component.CancelToken = null;
    }

    private void OnSolutionChange(EntityUid uid, IVBagComponent component, SolutionChangedEvent args)
    {
        Dirty(component);
    }

    private void OnInjectorGetState(EntityUid uid, IVBagComponent component, ref ComponentGetState args)
    {
        _solutions.TryGetSolution(uid, IVBagComponent.SolutionName, out var solution);

        var currentVolume = solution?.CurrentVolume ?? FixedPoint2.Zero;
        var maxVolume = solution?.MaxVolume ?? FixedPoint2.Zero;

        args.State = new SharedIVBagComponent.IVBagComponentState(currentVolume, maxVolume, component.ToggleState);
    }

    private void OnInjectorAfterInteract(EntityUid uid, IVBagComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach) return;

        if (component.CancelToken != null)
        {
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

    private void OnInjectorStartup(EntityUid uid, IVBagComponent component, ComponentStartup args)
    {
        Dirty(component);
    }

    private void OnInjectorUse(EntityUid uid, IVBagComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        Toggle(component, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Toggle between draw/inject state if applicable
    /// </summary>
    private void Toggle(IVBagComponent component, EntityUid user)
    {
        if (component.InjectOnly)
        {
            return;
        }

        string msg;
        switch (component.ToggleState)
        {
            case SharedIVBagComponent.IVBagToggleMode.Inject:
                component.ToggleState = SharedIVBagComponent.IVBagToggleMode.Draw;
                msg = "injector-component-drawing-text";
                break;
            case SharedIVBagComponent.IVBagToggleMode.Draw:
                component.ToggleState = SharedIVBagComponent.IVBagToggleMode.Inject;
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
    private void InjectDoAfter(IVBagComponent component, EntityUid user, EntityUid target)
    {
        // Create a pop-up for the user
        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, Filter.Entities(user));

        if (!_solutions.TryGetSolution(component.Owner, IVBagComponent.SolutionName, out var solution))
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
            if (component.ToggleState == SharedIVBagComponent.IVBagToggleMode.Inject)
            {
                _logs.Add(LogType.ForceFeed,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to inject {EntityManager.ToPrettyString(target):target} with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}");
            }
        }
        else
        {
            // Self-injections take half as long.
            actualDelay /= 2;

            if (component.ToggleState == SharedIVBagComponent.IVBagToggleMode.Inject)
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
            BroadcastFinishedEvent = new BagInjectionCompleteEvent()
            {
                Component = component,
                User = user,
                Target = target,
            },
            BroadcastCancelledEvent = new BagInjectionCancelledEvent()
            {
                Component = component,
            }
        });
    }

    private void TryInjectIntoBloodstream(IVBagComponent component, BloodstreamComponent targetBloodstream, EntityUid user)
    {
        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetBloodstream.ChemicalSolution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-cannot-inject-message", ("target", targetBloodstream.Owner)),
                component.Owner, Filter.Entities(user));
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutions.SplitSolution(user, targetBloodstream.ChemicalSolution, realTransferAmount);

        _blood.TryAddToChemicals((targetBloodstream).Owner, removedSolution, targetBloodstream);

        removedSolution.DoEntityReaction(targetBloodstream.Owner, ReactionMethod.Injection);

        _popup.PopupEntity(Loc.GetString("injector-component-inject-success-message",
                ("amount", removedSolution.TotalVolume),
                ("target", targetBloodstream.Owner)), component.Owner, Filter.Entities(user));

        Dirty(component);
        AfterInject(component);
    }

    private void TryInject(IVBagComponent component, EntityUid targetEntity, Solution targetSolution, EntityUid user, bool asRefill)
    {
        if (!_solutions.TryGetSolution(component.Owner, IVBagComponent.SolutionName, out var solution)
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

    private void AfterInject(IVBagComponent component)
    {
        // Automatically set syringe to draw after completely draining it.
        if (_solutions.TryGetSolution(component.Owner, IVBagComponent.SolutionName, out var solution)
            && solution.CurrentVolume == 0)
        {
            component.ToggleState = SharedIVBagComponent.IVBagToggleMode.Draw;
        }
    }

    private void AfterDraw(IVBagComponent component)
    {
        // Automatically set syringe to inject after completely filling it.
        if (_solutions.TryGetSolution(component.Owner, IVBagComponent.SolutionName, out var solution)
            && solution.AvailableVolume == 0)
        {
            component.ToggleState = SharedIVBagComponent.IVBagToggleMode.Inject;
        }
    }

    private void TryDraw(IVBagComponent component, EntityUid targetEntity, Solution targetSolution, EntityUid user)
    {
        if (!_solutions.TryGetSolution(component.Owner, IVBagComponent.SolutionName, out var solution)
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

    private sealed class BagInjectionCompleteEvent : EntityEventArgs
    {
        public IVBagComponent Component { get; init; } = default!;
        public EntityUid User { get; init; }
        public EntityUid Target { get; init; }
    }

    private sealed class BagInjectionCancelledEvent : EntityEventArgs
    {
        public IVBagComponent Component { get; init; } = default!;
    }
}
