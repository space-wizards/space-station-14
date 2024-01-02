using Content.Server.Body.Components;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem
{

    /// <summary>
    ///     Default transfer amounts for the set-transfer verb.
    /// </summary>
    public static readonly List<int> TransferAmounts = new() { 1, 5, 10, 15 };
    private void InitializeInjector()
    {
        SubscribeLocalEvent<InjectorComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
        SubscribeLocalEvent<InjectorComponent, SolutionContainerChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<InjectorComponent, InjectorDoAfterEvent>(OnInjectDoAfter);
        SubscribeLocalEvent<InjectorComponent, ComponentStartup>(OnInjectorStartup);
        SubscribeLocalEvent<InjectorComponent, UseInHandEvent>(OnInjectorUse);
        SubscribeLocalEvent<InjectorComponent, AfterInteractEvent>(OnInjectorAfterInteract);
        SubscribeLocalEvent<InjectorComponent, ComponentGetState>(OnInjectorGetState);
    }

    private void AddSetTransferVerbs(Entity<InjectorComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var (uid, component) = entity;

        // Add specific transfer verbs according to the container's size
        var priority = 0;
        var user = args.User;
        foreach (var amount in TransferAmounts)
        {
            if (amount < component.MinimumTransferAmount.Int() || amount > component.MaximumTransferAmount.Int())
                continue;

            AlternativeVerb verb = new();
            verb.Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount));
            verb.Category = VerbCategory.SetTransferAmount;
            verb.Act = () =>
            {
                component.TransferAmount = FixedPoint2.New(amount);
                _popup.PopupEntity(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)), user, user);
            };

            // we want to sort by size, not alphabetically by the verb text.
            verb.Priority = priority;
            priority--;

            args.Verbs.Add(verb);
        }
    }

    private void UseInjector(Entity<InjectorComponent> injector, EntityUid target, EntityUid user)
    {
        // Handle injecting/drawing for solutions
        if (injector.Comp.ToggleState == SharedInjectorComponent.InjectorToggleMode.Inject)
        {
            if (_solutionContainers.TryGetInjectableSolution(target, out var injectableSolution, out _))
            {
                TryInject(injector, target, injectableSolution.Value, user, false);
            }
            else if (_solutionContainers.TryGetRefillableSolution(target, out var refillableSolution, out _))
            {
                TryInject(injector, target, refillableSolution.Value, user, true);
            }
            else if (TryComp<BloodstreamComponent>(target, out var bloodstream))
            {
                TryInjectIntoBloodstream(injector, (target, bloodstream), user);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("injector-component-cannot-transfer-message",
                    ("target", Identity.Entity(target, EntityManager))), injector, user);
            }
        }
        else if (injector.Comp.ToggleState == SharedInjectorComponent.InjectorToggleMode.Draw)
        {
            // Draw from a bloodstream, if the target has that
            if (TryComp<BloodstreamComponent>(target, out var stream) &&
                _solutionContainers.ResolveSolution(target, stream.BloodSolutionName, ref stream.BloodSolution))
            {
                TryDraw(injector, (target, stream), stream.BloodSolution.Value, user);
                return;
            }

            // Draw from an object (food, beaker, etc)
            if (_solutionContainers.TryGetDrawableSolution(target, out var drawableSolution, out _))
            {
                TryDraw(injector, target, drawableSolution.Value, user);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("injector-component-cannot-draw-message",
                    ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);
            }
        }
    }

    private void OnSolutionChange(Entity<InjectorComponent> entity, ref SolutionContainerChangedEvent args)
    {
        Dirty(entity);
    }

    private void OnInjectorGetState(Entity<InjectorComponent> entity, ref ComponentGetState args)
    {
        _solutionContainers.TryGetSolution(entity.Owner, InjectorComponent.SolutionName, out _, out var solution);

        var currentVolume = solution?.Volume ?? FixedPoint2.Zero;
        var maxVolume = solution?.MaxVolume ?? FixedPoint2.Zero;

        args.State = new SharedInjectorComponent.InjectorComponentState(currentVolume, maxVolume, entity.Comp.ToggleState);
    }

    private void OnInjectDoAfter(Entity<InjectorComponent> entity, ref InjectorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        UseInjector(entity, args.Args.Target.Value, args.Args.User);
        args.Handled = true;
    }

    private void OnInjectorAfterInteract(Entity<InjectorComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        //Make sure we have the attacking entity
        if (args.Target is not { Valid: true } target || !HasComp<SolutionContainerManagerComponent>(entity))
            return;

        // Is the target a mob? If yes, use a do-after to give them time to respond.
        if (HasComp<MobStateComponent>(target) || HasComp<BloodstreamComponent>(target))
        {
            // Are use using an injector capible of targeting a mob?
            if (entity.Comp.IgnoreMobs)
                return;

            InjectDoAfter(entity, target, args.User);
            args.Handled = true;
            return;
        }

        UseInjector(entity, target, args.User);
        args.Handled = true;
    }

    private void OnInjectorStartup(Entity<InjectorComponent> entity, ref ComponentStartup args)
    {
        // ???? why ?????
        Dirty(entity);
    }

    private void OnInjectorUse(Entity<InjectorComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Toggle(entity, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Toggle between draw/inject state if applicable
    /// </summary>
    private void Toggle(Entity<InjectorComponent> injector, EntityUid user)
    {
        if (injector.Comp.InjectOnly)
        {
            return;
        }

        string msg;
        switch (injector.Comp.ToggleState)
        {
            case SharedInjectorComponent.InjectorToggleMode.Inject:
                injector.Comp.ToggleState = SharedInjectorComponent.InjectorToggleMode.Draw;
                msg = "injector-component-drawing-text";
                break;
            case SharedInjectorComponent.InjectorToggleMode.Draw:
                injector.Comp.ToggleState = SharedInjectorComponent.InjectorToggleMode.Inject;
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
    private void InjectDoAfter(Entity<InjectorComponent> injector, EntityUid target, EntityUid user)
    {
        // Create a pop-up for the user
        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);

        if (!_solutionContainers.TryGetSolution(injector.Owner, InjectorComponent.SolutionName, out _, out var solution))
            return;

        var actualDelay = MathF.Max(injector.Comp.Delay, 1f);

        // Injections take 0.5 seconds longer per additional 5u
        actualDelay += (float) injector.Comp.TransferAmount / injector.Comp.Delay - 0.5f;

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
            if (injector.Comp.ToggleState == SharedInjectorComponent.InjectorToggleMode.Inject)
            {
                _adminLogger.Add(LogType.ForceFeed,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to inject {EntityManager.ToPrettyString(target):target} with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}");
            }
        }
        else
        {
            // Self-injections take half as long.
            actualDelay /= 2;

            if (injector.Comp.ToggleState == SharedInjectorComponent.InjectorToggleMode.Inject)
                _adminLogger.Add(LogType.Ingestion, $"{EntityManager.ToPrettyString(user):user} is attempting to inject themselves with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}.");
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, actualDelay, new InjectorDoAfterEvent(), injector.Owner, target: target, used: injector.Owner)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            MovementThreshold = 0.1f,
        });
    }

    private void TryInjectIntoBloodstream(Entity<InjectorComponent> injector, Entity<BloodstreamComponent> target, EntityUid user)
    {
        // Get transfer amount. May be smaller than _transferAmount if not enough room
        if (!_solutionContainers.ResolveSolution(target.Owner, target.Comp.ChemicalSolutionName, ref target.Comp.ChemicalSolution, out var chemSolution))
        {
            _popup.PopupEntity(Loc.GetString("injector-component-cannot-inject-message", ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);
            return;
        }

        var realTransferAmount = FixedPoint2.Min(injector.Comp.TransferAmount, chemSolution.AvailableVolume);
        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-cannot-inject-message", ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutionContainers.SplitSolution(target.Comp.ChemicalSolution.Value, realTransferAmount);

        _blood.TryAddToChemicals(target, removedSolution, target.Comp);

        _reactiveSystem.DoEntityReaction(target, removedSolution, ReactionMethod.Injection);

        _popup.PopupEntity(Loc.GetString("injector-component-inject-success-message",
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);

        Dirty(injector);
        AfterInject(injector, target);
    }

    private void TryInject(Entity<InjectorComponent> injector, EntityUid targetEntity, Entity<SolutionComponent> targetSolution, EntityUid user, bool asRefill)
    {
        if (!_solutionContainers.TryGetSolution(injector.Owner, InjectorComponent.SolutionName, out var soln, out var solution) || solution.Volume == 0)
            return;

        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount = FixedPoint2.Min(injector.Comp.TransferAmount, targetSolution.Comp.Solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-target-already-full-message", ("target", Identity.Entity(targetEntity, EntityManager))),
                injector.Owner, user);
            return;
        }

        // Move units from attackSolution to targetSolution
        Solution removedSolution;
        if (TryComp<StackComponent>(targetEntity, out var stack))
            removedSolution = _solutionContainers.SplitStackSolution(soln.Value, realTransferAmount, stack.Count);
        else
            removedSolution = _solutionContainers.SplitSolution(soln.Value, realTransferAmount);

        _reactiveSystem.DoEntityReaction(targetEntity, removedSolution, ReactionMethod.Injection);

        if (!asRefill)
            _solutionContainers.Inject(targetEntity, targetSolution, removedSolution);
        else
            _solutionContainers.Refill(targetEntity, targetSolution, removedSolution);

        _popup.PopupEntity(Loc.GetString("injector-component-transfer-success-message",
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(targetEntity, EntityManager))), injector.Owner, user);

        Dirty(injector);
        AfterInject(injector, targetEntity);
    }

    private void AfterInject(Entity<InjectorComponent> injector, EntityUid target)
    {
        // Automatically set syringe to draw after completely draining it.
        if (_solutionContainers.TryGetSolution(injector.Owner, InjectorComponent.SolutionName, out _, out var solution) && solution.Volume == 0)
        {
            injector.Comp.ToggleState = SharedInjectorComponent.InjectorToggleMode.Draw;
        }

        // Leave some DNA from the injectee on it
        var ev = new TransferDnaEvent { Donor = target, Recipient = injector };
        RaiseLocalEvent(target, ref ev);
    }

    private void AfterDraw(Entity<InjectorComponent> injector, EntityUid target)
    {
        // Automatically set syringe to inject after completely filling it.
        if (_solutionContainers.TryGetSolution(injector.Owner, InjectorComponent.SolutionName, out _, out var solution) && solution.AvailableVolume == 0)
        {
            injector.Comp.ToggleState = SharedInjectorComponent.InjectorToggleMode.Inject;
        }

        // Leave some DNA from the drawee on it
        var ev = new TransferDnaEvent { Donor = target, Recipient = injector };
        RaiseLocalEvent(target, ref ev);
    }

    private void TryDraw(Entity<InjectorComponent> injector, Entity<BloodstreamComponent?> target, Entity<SolutionComponent> targetSolution, EntityUid user)
    {
        if (!_solutionContainers.TryGetSolution(injector.Owner, InjectorComponent.SolutionName, out var soln, out var solution) || solution.AvailableVolume == 0)
        {
            return;
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room, also make sure there's room in the injector
        var realTransferAmount = FixedPoint2.Min(injector.Comp.TransferAmount, targetSolution.Comp.Solution.Volume, solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("injector-component-target-is-empty-message", ("target", Identity.Entity(target, EntityManager))),
                injector.Owner, user);
            return;
        }

        // We have some snowflaked behavior for streams.
        if (target.Comp != null)
        {
            DrawFromBlood(injector, (target.Owner, target.Comp), soln.Value, realTransferAmount, user);
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = _solutionContainers.Draw(target.Owner, targetSolution, realTransferAmount);

        if (!_solutionContainers.TryAddSolution(soln.Value, removedSolution))
        {
            return;
        }

        _popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);

        Dirty(injector);
        AfterDraw(injector, target);
    }

    private void DrawFromBlood(Entity<InjectorComponent> injector, Entity<BloodstreamComponent> target, Entity<SolutionComponent> injectorSolution, FixedPoint2 transferAmount, EntityUid user)
    {
        var drawAmount = (float) transferAmount;

        if (_solutionContainers.ResolveSolution(target.Owner, target.Comp.ChemicalSolutionName, ref target.Comp.ChemicalSolution))
        {
            var chemTemp = _solutionContainers.SplitSolution(target.Comp.ChemicalSolution.Value, drawAmount * 0.15f);
            _solutionContainers.TryAddSolution(injectorSolution, chemTemp);
            drawAmount -= (float) chemTemp.Volume;
        }

        if (_solutionContainers.ResolveSolution(target.Owner, target.Comp.BloodSolutionName, ref target.Comp.BloodSolution))
        {
            var bloodTemp = _solutionContainers.SplitSolution(target.Comp.BloodSolution.Value, drawAmount);
            _solutionContainers.TryAddSolution(injectorSolution, bloodTemp);
        }

        _popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
                ("amount", transferAmount),
                ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);

        Dirty(injector);
        AfterDraw(injector, target);
    }
}
