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
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using System.Threading;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Content.Shared.Popups;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem
{

    /// <summary>
    ///     Default transfer amounts for the set-transfer verb.
    /// </summary>
    public static readonly List<int> TransferAmounts = new() {1, 5, 10, 15};
    private void InitializeInjector()
    {
        SubscribeLocalEvent<InjectorComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
        SubscribeLocalEvent<InjectorComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<InjectorComponent, HandDeselectedEvent>(OnInjectorDeselected);
        SubscribeLocalEvent<InjectorComponent, ComponentStartup>(OnInjectorStartup);
        SubscribeLocalEvent<InjectorComponent, UseInHandEvent>(OnInjectorUse);
        SubscribeLocalEvent<InjectorComponent, AfterInteractEvent>(OnInjectorAfterInteract);
        SubscribeLocalEvent<InjectorComponent, ComponentGetState>(OnInjectorGetState);

        SubscribeLocalEvent<InjectionCompleteEvent>(OnInjectionComplete);
        SubscribeLocalEvent<InjectionCancelledEvent>(OnInjectionCancelled);
    }

    private void AddSetTransferVerbs(EntityUid uid, InjectorComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
            return;

        // Add specific transfer verbs according to the container's size
        var priority = 0;
        foreach (var amount in TransferAmounts)
        {
            if ( amount < component.MinimumTransferAmount.Int() || amount > component.MaximumTransferAmount.Int())
                continue;

            AlternativeVerb verb = new();
            verb.Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount));
            verb.Category = VerbCategory.SetTransferAmount;
            verb.Act = () =>
            {
                component.TransferAmount = FixedPoint2.New(amount);
                args.User.PopupMessage(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)));
            };

            // we want to sort by size, not alphabetically by the verb text.
            verb.Priority = priority;
            priority--;

            args.Verbs.Add(verb);
        }
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
                    ("target", Identity.Entity(target, EntityManager))), component.Owner, Filter.Entities(user));
            }
        }
        else if (component.ToggleState == SharedInjectorComponent.InjectorToggleMode.Draw)
        {
            // Draw from a bloodstream, if the target has that
            if (TryComp<BloodstreamComponent>(target, out var stream))
            {
                TryDraw(component, target, stream.BloodSolution, user, stream);
                return;
            }

            // Draw from an object (food, beaker, etc)
            if (_solutions.TryGetDrawableSolution(target, out var drawableSolution))
            {
                TryDraw(component, target, drawableSolution, user);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("injector-component-cannot-draw-message",
                    ("target", Identity.Entity(target, EntityManager))), component.Owner, Filter.Entities(user));
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
        if (args.Handled || !args.CanReach)
            return;

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
            // Are use using an injector capible of targeting a mob?
            if (component.IgnoreMobs)
                return;

            InjectDoAfter(component, args.User, target);
            args.Handled = true;
            return;
        }

        UseInjector(target, args.User, component);
        args.Handled = true;
    }

    private void OnInjectorStartup(EntityUid uid, InjectorComponent component, ComponentStartup args)
    {
        /// ???? why ?????
        Dirty(component);
    }

    private void OnInjectorUse(EntityUid uid, InjectorComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

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

        // Injections take 1 second longer per additional 5u
        actualDelay += (float) component.TransferAmount / component.Delay - 1;

        if (user != target)
        {
            // Create a pop-up for the target
            var userName = Identity.Entity(user, EntityManager);
            _popup.PopupEntity(Loc.GetString("injector-component-injecting-target",
                ("user", userName)), user, Filter.Entities(target));

            // Check if the target is incapacitated or in combat mode and modify time accordingly.
            if (_mobState.IsIncapacitated(target))
            {
                actualDelay /= 2;
            }
            else if (_combat.IsInCombatMode(target))
            {
                // Slightly increase the delay when the target is in combat mode. Helps prevents cheese injections in
                // combat with fast syringes & lag.
                actualDelay += 1;
            }

            // Add an admin log, using the "force feed" log type. It's not quite feeding, but the effect is the same.
            if (component.ToggleState == SharedInjectorComponent.InjectorToggleMode.Inject)
            {
                _adminLogger.Add(LogType.ForceFeed,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to inject {EntityManager.ToPrettyString(target):target} with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}");
            }
        }
        else
        {
            // Self-injections take half as long.
            actualDelay /= 2;

            if (component.ToggleState == SharedInjectorComponent.InjectorToggleMode.Inject)
                _adminLogger.Add(LogType.Ingestion,
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
        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetBloodstream.ChemicalSolution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-cannot-inject-message", ("target", Identity.Entity(targetBloodstream.Owner, EntityManager))),
                component.Owner, Filter.Entities(user));
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutions.SplitSolution(user, targetBloodstream.ChemicalSolution, realTransferAmount);

        _blood.TryAddToChemicals((targetBloodstream).Owner, removedSolution, targetBloodstream);

        removedSolution.DoEntityReaction(targetBloodstream.Owner, ReactionMethod.Injection);

        _popup.PopupEntity(Loc.GetString("injector-component-inject-success-message",
                ("amount", removedSolution.TotalVolume),
                ("target", Identity.Entity(targetBloodstream.Owner, EntityManager))), component.Owner, Filter.Entities(user));

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
            _popup.PopupEntity(Loc.GetString("injector-component-target-already-full-message", ("target", Identity.Entity(targetEntity, EntityManager))),
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
                ("target", Identity.Entity(targetEntity, EntityManager))), component.Owner, Filter.Entities(user));

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

    private void TryDraw(InjectorComponent component, EntityUid targetEntity, Solution targetSolution, EntityUid user, BloodstreamComponent? stream = null)
    {
        if (!_solutions.TryGetSolution(component.Owner, InjectorComponent.SolutionName, out var solution)
            || solution.AvailableVolume == 0)
        {
            return;
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room, also make sure there's room in the injector
        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetSolution.DrawAvailable, solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-target-is-empty-message", ("target", Identity.Entity(targetEntity, EntityManager))),
                component.Owner, Filter.Entities(user));
            return;
        }

        // We have some snowflaked behavior for streams.
        if (stream != null)
        {
            DrawFromBlood(user, targetEntity, component, solution, stream, realTransferAmount);
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
                ("target", Identity.Entity(targetEntity, EntityManager))), component.Owner, Filter.Entities(user));

        Dirty(component);
        AfterDraw(component);
    }

    private void DrawFromBlood(EntityUid user, EntityUid target, InjectorComponent component, Solution injectorSolution, BloodstreamComponent stream, FixedPoint2 transferAmount)
    {
        var drawAmount = (float) transferAmount;
        var bloodAmount = drawAmount;
        var chemAmount = 0f;
        if (stream.ChemicalSolution.CurrentVolume > 0f) // If they have stuff in their chem stream, we'll draw some of that
        {
            bloodAmount = drawAmount * 0.85f;
            chemAmount = drawAmount * 0.15f;
        }

        var bloodTemp = stream.BloodSolution.SplitSolution(bloodAmount);
        var chemTemp = stream.ChemicalSolution.SplitSolution(chemAmount);

        _solutions.TryAddSolution(component.Owner, injectorSolution, bloodTemp);
        _solutions.TryAddSolution(component.Owner, injectorSolution, chemTemp);

        _popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
                ("amount", transferAmount),
                ("target", Identity.Entity(target, EntityManager))), component.Owner, Filter.Entities(user));

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
