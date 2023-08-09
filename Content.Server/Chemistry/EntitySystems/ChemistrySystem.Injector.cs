using Content.Server.Body.Components;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Content.Shared.Stacks;
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
        SubscribeLocalEvent<InjectorComponent, InjectorDoAfterEvent>(OnInjectDoAfter);
        SubscribeLocalEvent<InjectorComponent, ComponentStartup>(OnInjectorStartup);
        SubscribeLocalEvent<InjectorComponent, UseInHandEvent>(OnInjectorUse);
        SubscribeLocalEvent<InjectorComponent, AfterInteractEvent>(OnInjectorAfterInteract);
        SubscribeLocalEvent<InjectorComponent, ComponentGetState>(OnInjectorGetState);
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
                _popup.PopupEntity(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)), args.User, args.User);
            };

            // we want to sort by size, not alphabetically by the verb text.
            verb.Priority = priority;
            priority--;

            args.Verbs.Add(verb);
        }
    }

    private void UseInjector(EntityUid target, EntityUid user, EntityUid injector, InjectorComponent component)
    {
        // Handle injecting/drawing for solutions
        if (component.ToggleState == SharedInjectorComponent.InjectorToggleMode.Inject)
        {
            if (_solutions.TryGetInjectableSolution(target, out var injectableSolution))
            {
                TryInject(component, injector, target, injectableSolution, user, false);
            }
            else if (_solutions.TryGetRefillableSolution(target, out var refillableSolution))
            {
                TryInject(component, injector, target, refillableSolution, user, true);
            }
            else if (TryComp<BloodstreamComponent>(target, out var bloodstream))
            {
                TryInjectIntoBloodstream(component, injector, target, bloodstream, user);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("injector-component-cannot-transfer-message",
                    ("target", Identity.Entity(target, EntityManager))), injector, user);
            }
        }
        else if (component.ToggleState == SharedInjectorComponent.InjectorToggleMode.Draw)
        {
            // Draw from a bloodstream, if the target has that
            if (TryComp<BloodstreamComponent>(target, out var stream))
            {
                TryDraw(component, injector, target, stream.BloodSolution, user, stream);
                return;
            }

            // Draw from an object (food, beaker, etc)
            if (_solutions.TryGetDrawableSolution(target, out var drawableSolution))
            {
                TryDraw(component, injector, target, drawableSolution, user);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("injector-component-cannot-draw-message",
                    ("target", Identity.Entity(target, EntityManager))), injector, user);
            }
        }
    }

    private void OnSolutionChange(EntityUid uid, InjectorComponent component, SolutionChangedEvent args)
    {
        Dirty(component);
    }

    private void OnInjectorGetState(EntityUid uid, InjectorComponent component, ref ComponentGetState args)
    {
        _solutions.TryGetSolution(uid, InjectorComponent.SolutionName, out var solution);

        var currentVolume = solution?.Volume ?? FixedPoint2.Zero;
        var maxVolume = solution?.MaxVolume ?? FixedPoint2.Zero;

        args.State = new SharedInjectorComponent.InjectorComponentState(currentVolume, maxVolume, component.ToggleState);
    }

    private void OnInjectDoAfter(EntityUid uid, InjectorComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        UseInjector(args.Args.Target.Value, args.Args.User, uid, component);
        args.Handled = true;
    }

    private void OnInjectorAfterInteract(EntityUid uid, InjectorComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        //Make sure we have the attacking entity
        if (args.Target is not { Valid: true } target || !HasComp<SolutionContainerManagerComponent>(uid))
            return;

        // Is the target a mob? If yes, use a do-after to give them time to respond.
        if (HasComp<MobStateComponent>(target) || HasComp<BloodstreamComponent>(target))
        {
            // Are use using an injector capible of targeting a mob?
            if (component.IgnoreMobs)
                return;

            InjectDoAfter(component, args.User, target, uid);
            args.Handled = true;
            return;
        }

        UseInjector(target, args.User, uid, component);
        args.Handled = true;
    }

    private void OnInjectorStartup(EntityUid uid, InjectorComponent component, ComponentStartup args)
    {
        // ???? why ?????
        Dirty(component);
    }

    private void OnInjectorUse(EntityUid uid, InjectorComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Toggle(component, args.User, uid);
        args.Handled = true;
    }

    /// <summary>
    /// Toggle between draw/inject state if applicable
    /// </summary>
    private void Toggle(InjectorComponent component, EntityUid user, EntityUid injector)
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

        _popup.PopupEntity(Loc.GetString(msg), injector, user);
    }

    /// <summary>
    /// Send informative pop-up messages and wait for a do-after to complete.
    /// </summary>
    private void InjectDoAfter(InjectorComponent component, EntityUid user, EntityUid target, EntityUid injector)
    {
        // Create a pop-up for the user
        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);

        if (!_solutions.TryGetSolution(injector, InjectorComponent.SolutionName, out var solution))
            return;

        var actualDelay = MathF.Max(component.Delay, 1f);

        // Injections take 0.5 seconds longer per additional 5u
        actualDelay += (float) component.TransferAmount / component.Delay - 0.5f;

        var isTarget = user != target;

        if (isTarget)
        {
            // Create a pop-up for the target
            var userName = Identity.Entity(user, EntityManager);
            _popup.PopupEntity(Loc.GetString("injector-component-injecting-target",
                ("user", userName)), user, target);

            // Check if the target is incapacitated or in combat mode and modify time accordingly.
            if (_mobState.IsIncapacitated(target))
            {
                actualDelay /= 2.5f;
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
                _adminLogger.Add(LogType.Ingestion, $"{EntityManager.ToPrettyString(user):user} is attempting to inject themselves with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}.");
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(user, actualDelay, new InjectorDoAfterEvent(), injector, target: target, used: injector)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            MovementThreshold = 0.1f,
        });
    }

    private void TryInjectIntoBloodstream(InjectorComponent component, EntityUid injector, EntityUid target, BloodstreamComponent targetBloodstream, EntityUid user)
    {
        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetBloodstream.ChemicalSolution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-cannot-inject-message", ("target", Identity.Entity(target, EntityManager))), injector, user);
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutions.SplitSolution(user, targetBloodstream.ChemicalSolution, realTransferAmount);

        _blood.TryAddToChemicals(target, removedSolution, targetBloodstream);

        _reactiveSystem.DoEntityReaction(target, removedSolution, ReactionMethod.Injection);

        _popup.PopupEntity(Loc.GetString("injector-component-inject-success-message",
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(target, EntityManager))), injector, user);

        Dirty(component);
        AfterInject(component, injector);
    }

    private void TryInject(InjectorComponent component, EntityUid injector, EntityUid targetEntity, Solution targetSolution, EntityUid user, bool asRefill)
    {
        if (!_solutions.TryGetSolution(injector, InjectorComponent.SolutionName, out var solution)
            || solution.Volume == 0)
            return;

        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetSolution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-target-already-full-message", ("target", Identity.Entity(targetEntity, EntityManager))),
                injector, user);
            return;
        }

        // Move units from attackSolution to targetSolution
        Solution removedSolution;
        if (TryComp<StackComponent>(targetEntity, out var stack)) 
            removedSolution = _solutions.SplitStackSolution(injector, solution, realTransferAmount, stack.Count);
        else
          removedSolution = _solutions.SplitSolution(injector, solution, realTransferAmount);

        _reactiveSystem.DoEntityReaction(targetEntity, removedSolution, ReactionMethod.Injection);

        if (!asRefill)
            _solutions.Inject(targetEntity, targetSolution, removedSolution);
        else
            _solutions.Refill(targetEntity, targetSolution, removedSolution);

        _popup.PopupEntity(Loc.GetString("injector-component-transfer-success-message",
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(targetEntity, EntityManager))), injector, user);

        Dirty(component);
        AfterInject(component, injector);
    }

    private void AfterInject(InjectorComponent component, EntityUid injector)
    {
        // Automatically set syringe to draw after completely draining it.
        if (_solutions.TryGetSolution(injector, InjectorComponent.SolutionName, out var solution)
            && solution.Volume == 0)
        {
            component.ToggleState = SharedInjectorComponent.InjectorToggleMode.Draw;
        }
    }

    private void AfterDraw(InjectorComponent component, EntityUid injector)
    {
        // Automatically set syringe to inject after completely filling it.
        if (_solutions.TryGetSolution(injector, InjectorComponent.SolutionName, out var solution)
            && solution.AvailableVolume == 0)
        {
            component.ToggleState = SharedInjectorComponent.InjectorToggleMode.Inject;
        }
    }

    private void TryDraw(InjectorComponent component, EntityUid injector, EntityUid targetEntity, Solution targetSolution, EntityUid user, BloodstreamComponent? stream = null)
    {
        if (!_solutions.TryGetSolution(injector, InjectorComponent.SolutionName, out var solution)
            || solution.AvailableVolume == 0)
        {
            return;
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room, also make sure there's room in the injector
        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetSolution.Volume, solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-target-is-empty-message", ("target", Identity.Entity(targetEntity, EntityManager))),
                injector, user);
            return;
        }

        // We have some snowflaked behavior for streams.
        if (stream != null)
        {
            DrawFromBlood(user, injector, targetEntity, component, solution, stream, realTransferAmount);
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutions.Draw(targetEntity, targetSolution, realTransferAmount);

        if (!_solutions.TryAddSolution(injector, solution, removedSolution))
        {
            return;
        }

        _popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(targetEntity, EntityManager))), injector, user);

        Dirty(component);
        AfterDraw(component, injector);
    }

    private void DrawFromBlood(EntityUid user, EntityUid injector, EntityUid target, InjectorComponent component, Solution injectorSolution, BloodstreamComponent stream, FixedPoint2 transferAmount)
    {
        var drawAmount = (float) transferAmount;
        var bloodAmount = drawAmount;
        var chemAmount = 0f;
        if (stream.ChemicalSolution.Volume > 0f) // If they have stuff in their chem stream, we'll draw some of that
        {
            bloodAmount = drawAmount * 0.85f;
            chemAmount = drawAmount * 0.15f;
        }

        var bloodTemp = stream.BloodSolution.SplitSolution(bloodAmount);
        var chemTemp = stream.ChemicalSolution.SplitSolution(chemAmount);

        _solutions.TryAddSolution(injector, injectorSolution, bloodTemp);
        _solutions.TryAddSolution(injector, injectorSolution, chemTemp);

        _popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
                ("amount", transferAmount),
                ("target", Identity.Entity(target, EntityManager))), injector, user);

        Dirty(component);
        AfterDraw(component, injector);
    }

}
