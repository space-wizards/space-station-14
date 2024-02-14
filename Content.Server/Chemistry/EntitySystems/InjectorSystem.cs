using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry;
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
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class InjectorSystem : SharedInjectorSystem
{
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectorComponent, InjectorDoAfterEvent>(OnInjectDoAfter);
        SubscribeLocalEvent<InjectorComponent, AfterInteractEvent>(OnInjectorAfterInteract);
    }

    private void UseInjector(Entity<InjectorComponent> injector, EntityUid target, EntityUid user)
    {
        // Handle injecting/drawing for solutions
        if (injector.Comp.ToggleState == InjectorToggleMode.Inject)
        {
            if (SolutionContainers.TryGetInjectableSolution(target, out var injectableSolution, out _))
            {
                TryInject(injector, target, injectableSolution.Value, user, false);
            }
            else if (SolutionContainers.TryGetRefillableSolution(target, out var refillableSolution, out _))
            {
                TryInject(injector, target, refillableSolution.Value, user, true);
            }
            else if (TryComp<BloodstreamComponent>(target, out var bloodstream))
            {
                TryInjectIntoBloodstream(injector, (target, bloodstream), user);
            }
            else
            {
                Popup.PopupEntity(Loc.GetString("injector-component-cannot-transfer-message",
                    ("target", Identity.Entity(target, EntityManager))), injector, user);
            }
        }
        else if (injector.Comp.ToggleState == InjectorToggleMode.Draw)
        {
            // Draw from a bloodstream, if the target has that
            if (TryComp<BloodstreamComponent>(target, out var stream) &&
                SolutionContainers.ResolveSolution(target, stream.BloodSolutionName, ref stream.BloodSolution))
            {
                TryDraw(injector, (target, stream), stream.BloodSolution.Value, user);
                return;
            }

            // Draw from an object (food, beaker, etc)
            if (SolutionContainers.TryGetDrawableSolution(target, out var drawableSolution, out _))
            {
                TryDraw(injector, target, drawableSolution.Value, user);
            }
            else
            {
                Popup.PopupEntity(Loc.GetString("injector-component-cannot-draw-message",
                    ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);
            }
        }
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

    /// <summary>
    /// Send informative pop-up messages and wait for a do-after to complete.
    /// </summary>
    private void InjectDoAfter(Entity<InjectorComponent> injector, EntityUid target, EntityUid user)
    {
        // Create a pop-up for the user
        Popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);

        if (!SolutionContainers.TryGetSolution(injector.Owner, InjectorComponent.SolutionName, out _, out var solution))
            return;

        var actualDelay = MathHelper.Max(injector.Comp.Delay, TimeSpan.FromSeconds(1));

        // Injections take 0.5 seconds longer per additional 5u
        actualDelay += TimeSpan.FromSeconds(injector.Comp.TransferAmount.Float() / injector.Comp.Delay.TotalSeconds - 0.5f);

        var isTarget = user != target;

        if (isTarget)
        {
            // Create a pop-up for the target
            var userName = Identity.Entity(user, EntityManager);
            Popup.PopupEntity(Loc.GetString("injector-component-injecting-target",
                ("user", userName)), user, target);

            // Check if the target is incapacitated or in combat mode and modify time accordingly.
            if (MobState.IsIncapacitated(target))
            {
                actualDelay /= 2.5f;
            }
            else if (Combat.IsInCombatMode(target))
            {
                // Slightly increase the delay when the target is in combat mode. Helps prevents cheese injections in
                // combat with fast syringes & lag.
                actualDelay += TimeSpan.FromSeconds(1);
            }

            // Add an admin log, using the "force feed" log type. It's not quite feeding, but the effect is the same.
            if (injector.Comp.ToggleState == InjectorToggleMode.Inject)
            {
                AdminLogger.Add(LogType.ForceFeed,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to inject {EntityManager.ToPrettyString(target):target} with a solution {SharedSolutionContainerSystem.ToPrettyString(solution):solution}");
            }
            else
            {
                AdminLogger.Add(LogType.ForceFeed,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to draw {injector.Comp.TransferAmount.ToString()} units from {EntityManager.ToPrettyString(target):target}");
            }
        }
        else
        {
            // Self-injections take half as long.
            actualDelay /= 2;

            if (injector.Comp.ToggleState == InjectorToggleMode.Inject)
            {
                AdminLogger.Add(LogType.Ingestion,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to inject themselves with a solution {SharedSolutionContainerSystem.ToPrettyString(solution):solution}.");
            }
            else
            {
                AdminLogger.Add(LogType.ForceFeed,
                    $"{EntityManager.ToPrettyString(user):user} is attempting to draw {injector.Comp.TransferAmount.ToString()} units from themselves.");
            }
        }

        DoAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, actualDelay, new InjectorDoAfterEvent(), injector.Owner, target: target, used: injector.Owner)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            MovementThreshold = 0.1f,
        });
    }

    private void TryInjectIntoBloodstream(Entity<InjectorComponent> injector, Entity<BloodstreamComponent> target,
        EntityUid user)
    {
        // Get transfer amount. May be smaller than _transferAmount if not enough room
        if (!SolutionContainers.ResolveSolution(target.Owner, target.Comp.ChemicalSolutionName,
                ref target.Comp.ChemicalSolution, out var chemSolution))
        {
            Popup.PopupEntity(
                Loc.GetString("injector-component-cannot-inject-message",
                    ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);
            return;
        }

        var realTransferAmount = FixedPoint2.Min(injector.Comp.TransferAmount, chemSolution.AvailableVolume);
        if (realTransferAmount <= 0)
        {
            Popup.PopupEntity(
                Loc.GetString("injector-component-cannot-inject-message",
                    ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);
            return;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = SolutionContainers.SplitSolution(target.Comp.ChemicalSolution.Value, realTransferAmount);

        _blood.TryAddToChemicals(target, removedSolution, target.Comp);

        _reactiveSystem.DoEntityReaction(target, removedSolution, ReactionMethod.Injection);

        Popup.PopupEntity(Loc.GetString("injector-component-inject-success-message",
            ("amount", removedSolution.Volume),
            ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);

        Dirty(injector);
        AfterInject(injector, target);
    }

    private void TryInject(Entity<InjectorComponent> injector, EntityUid targetEntity,
        Entity<SolutionComponent> targetSolution, EntityUid user, bool asRefill)
    {
        if (!SolutionContainers.TryGetSolution(injector.Owner, InjectorComponent.SolutionName, out var soln,
                out var solution) || solution.Volume == 0)
            return;

        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount =
            FixedPoint2.Min(injector.Comp.TransferAmount, targetSolution.Comp.Solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            Popup.PopupEntity(
                Loc.GetString("injector-component-target-already-full-message",
                    ("target", Identity.Entity(targetEntity, EntityManager))),
                injector.Owner, user);
            return;
        }

        // Move units from attackSolution to targetSolution
        Solution removedSolution;
        if (TryComp<StackComponent>(targetEntity, out var stack))
            removedSolution = SolutionContainers.SplitStackSolution(soln.Value, realTransferAmount, stack.Count);
        else
            removedSolution = SolutionContainers.SplitSolution(soln.Value, realTransferAmount);

        _reactiveSystem.DoEntityReaction(targetEntity, removedSolution, ReactionMethod.Injection);

        if (!asRefill)
            SolutionContainers.Inject(targetEntity, targetSolution, removedSolution);
        else
            SolutionContainers.Refill(targetEntity, targetSolution, removedSolution);

        Popup.PopupEntity(Loc.GetString("injector-component-transfer-success-message",
            ("amount", removedSolution.Volume),
            ("target", Identity.Entity(targetEntity, EntityManager))), injector.Owner, user);

        Dirty(injector);
        AfterInject(injector, targetEntity);
    }

    private void AfterInject(Entity<InjectorComponent> injector, EntityUid target)
    {
        // Automatically set syringe to draw after completely draining it.
        if (SolutionContainers.TryGetSolution(injector.Owner, InjectorComponent.SolutionName, out _,
                out var solution) && solution.Volume == 0)
        {
            SetMode(injector, InjectorToggleMode.Draw);
        }

        // Leave some DNA from the injectee on it
        var ev = new TransferDnaEvent { Donor = target, Recipient = injector };
        RaiseLocalEvent(target, ref ev);
    }

    private void AfterDraw(Entity<InjectorComponent> injector, EntityUid target)
    {
        // Automatically set syringe to inject after completely filling it.
        if (SolutionContainers.TryGetSolution(injector.Owner, InjectorComponent.SolutionName, out _,
                out var solution) && solution.AvailableVolume == 0)
        {
            SetMode(injector, InjectorToggleMode.Inject);
        }

        // Leave some DNA from the drawee on it
        var ev = new TransferDnaEvent { Donor = target, Recipient = injector };
        RaiseLocalEvent(target, ref ev);
    }

    private void TryDraw(Entity<InjectorComponent> injector, Entity<BloodstreamComponent?> target,
        Entity<SolutionComponent> targetSolution, EntityUid user)
    {
        if (!SolutionContainers.TryGetSolution(injector.Owner, InjectorComponent.SolutionName, out var soln,
                out var solution) || solution.AvailableVolume == 0)
        {
            return;
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room, also make sure there's room in the injector
        var realTransferAmount = FixedPoint2.Min(injector.Comp.TransferAmount, targetSolution.Comp.Solution.Volume,
            solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            Popup.PopupEntity(
                Loc.GetString("injector-component-target-is-empty-message",
                    ("target", Identity.Entity(target, EntityManager))),
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
        var removedSolution = SolutionContainers.Draw(target.Owner, targetSolution, realTransferAmount);

        if (!SolutionContainers.TryAddSolution(soln.Value, removedSolution))
        {
            return;
        }

        Popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
            ("amount", removedSolution.Volume),
            ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);

        Dirty(injector);
        AfterDraw(injector, target);
    }

    private void DrawFromBlood(Entity<InjectorComponent> injector, Entity<BloodstreamComponent> target,
        Entity<SolutionComponent> injectorSolution, FixedPoint2 transferAmount, EntityUid user)
    {
        var drawAmount = (float) transferAmount;

        if (SolutionContainers.ResolveSolution(target.Owner, target.Comp.ChemicalSolutionName,
                ref target.Comp.ChemicalSolution))
        {
            var chemTemp = SolutionContainers.SplitSolution(target.Comp.ChemicalSolution.Value, drawAmount * 0.15f);
            SolutionContainers.TryAddSolution(injectorSolution, chemTemp);
            drawAmount -= (float) chemTemp.Volume;
        }

        if (SolutionContainers.ResolveSolution(target.Owner, target.Comp.BloodSolutionName,
                ref target.Comp.BloodSolution))
        {
            var bloodTemp = SolutionContainers.SplitSolution(target.Comp.BloodSolution.Value, drawAmount);
            SolutionContainers.TryAddSolution(injectorSolution, bloodTemp);
        }

        Popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
            ("amount", transferAmount),
            ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);

        Dirty(injector);
        AfterDraw(injector, target);
    }
}
