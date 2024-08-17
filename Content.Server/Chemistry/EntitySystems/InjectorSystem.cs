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

    private bool TryUseInjector(Entity<InjectorComponent> injector, EntityUid target, EntityUid user)
    {
        // Handle injecting/drawing for solutions
        if (injector.Comp.ToggleState == InjectorToggleMode.Inject)
        {
            if (SolutionContainers.TryGetInjectableSolution(target, out var injectableSolution, out _))
                return TryInject(injector, target, injectableSolution.Value, user, false);

            if (SolutionContainers.TryGetRefillableSolution(target, out var refillableSolution, out _))
                return TryInject(injector, target, refillableSolution.Value, user, true);

            if (TryComp<BloodstreamComponent>(target, out var bloodstream))
                return TryInjectIntoBloodstream(injector, (target, bloodstream), user);

            Popup.PopupEntity(Loc.GetString("injector-component-cannot-transfer-message",
                ("target", Identity.Entity(target, EntityManager))), injector, user);
            return false;
        }

        if (injector.Comp.ToggleState == InjectorToggleMode.Draw)
        {
            // Draw from a bloodstream, if the target has that
            if (TryComp<BloodstreamComponent>(target, out var stream) &&
                SolutionContainers.ResolveSolution(target, stream.BloodSolutionName, ref stream.BloodSolution))
            {
                return TryDraw(injector, (target, stream), stream.BloodSolution.Value, user);
            }

            // Draw from an object (food, beaker, etc)
            if (SolutionContainers.TryGetDrawableSolution(target, out var drawableSolution, out _))
                return TryDraw(injector, target, drawableSolution.Value, user);

            Popup.PopupEntity(Loc.GetString("injector-component-cannot-draw-message",
                ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);
            return false;
        }
        return false;
    }

    private void OnInjectDoAfter(Entity<InjectorComponent> entity, ref InjectorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        args.Handled = TryUseInjector(entity, args.Args.Target.Value, args.Args.User);
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

        args.Handled = TryUseInjector(entity, target, args.User);
    }

    /// <summary>
    /// Send informative pop-up messages and wait for a do-after to complete.
    /// </summary>
    private void InjectDoAfter(Entity<InjectorComponent> injector, EntityUid target, EntityUid user)
    {
        // Create a pop-up for the user
        if (injector.Comp.ToggleState == InjectorToggleMode.Draw)
        {
            Popup.PopupEntity(Loc.GetString("injector-component-drawing-user"), target, user);
        }
        else
        {
            Popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);
        }

        if (!SolutionContainers.TryGetSolution(injector.Owner, injector.Comp.SolutionName, out _, out var solution))
            return;

        var actualDelay = injector.Comp.Delay;
        FixedPoint2 amountToInject;
        if (injector.Comp.ToggleState == InjectorToggleMode.Draw)
        {
            // additional delay is based on actual volume left to draw in syringe when smaller than transfer amount
            amountToInject = FixedPoint2.Min(injector.Comp.TransferAmount, (solution.MaxVolume - solution.Volume));
        }
        else
        {
            // additional delay is based on actual volume left to inject in syringe when smaller than transfer amount
            amountToInject = FixedPoint2.Min(injector.Comp.TransferAmount, solution.Volume);
        }

        // Injections take 0.5 seconds longer per 5u of possible space/content
        // First 5u(MinimumTransferAmount) doesn't incur delay
        actualDelay += injector.Comp.DelayPerVolume * FixedPoint2.Max(0, amountToInject - injector.Comp.MinimumTransferAmount).Double();

        // Ensure that minimum delay before incapacitation checks is 1 seconds
        actualDelay = MathHelper.Max(actualDelay, TimeSpan.FromSeconds(1));


        var isTarget = user != target;

        if (isTarget)
        {
            // Create a pop-up for the target
            var userName = Identity.Entity(user, EntityManager);
            if (injector.Comp.ToggleState == InjectorToggleMode.Draw)
            {
                Popup.PopupEntity(Loc.GetString("injector-component-drawing-target",
    ("user", userName)), user, target);
            }
            else
            {
                Popup.PopupEntity(Loc.GetString("injector-component-injecting-target",
    ("user", userName)), user, target);
            }


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
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = injector.Comp.NeedHand,
            BreakOnHandChange = injector.Comp.BreakOnHandChange,
            MovementThreshold = injector.Comp.MovementThreshold,
        });
    }

    private bool TryInjectIntoBloodstream(Entity<InjectorComponent> injector, Entity<BloodstreamComponent> target,
        EntityUid user)
    {
        // Get transfer amount. May be smaller than _transferAmount if not enough room
        if (!SolutionContainers.ResolveSolution(target.Owner, target.Comp.ChemicalSolutionName,
                ref target.Comp.ChemicalSolution, out var chemSolution))
        {
            Popup.PopupEntity(
                Loc.GetString("injector-component-cannot-inject-message",
                    ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);
            return false;
        }

        var realTransferAmount = FixedPoint2.Min(injector.Comp.TransferAmount, chemSolution.AvailableVolume);
        if (realTransferAmount <= 0)
        {
            Popup.PopupEntity(
                Loc.GetString("injector-component-cannot-inject-message",
                    ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);
            return false;
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
        return true;
    }

    private bool TryInject(Entity<InjectorComponent> injector, EntityUid targetEntity,
        Entity<SolutionComponent> targetSolution, EntityUid user, bool asRefill)
    {
        if (!SolutionContainers.TryGetSolution(injector.Owner, injector.Comp.SolutionName, out var soln,
                out var solution) || solution.Volume == 0)
            return false;

        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount =
            FixedPoint2.Min(injector.Comp.TransferAmount, targetSolution.Comp.Solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            Popup.PopupEntity(
                Loc.GetString("injector-component-target-already-full-message",
                    ("target", Identity.Entity(targetEntity, EntityManager))),
                injector.Owner, user);
            return false;
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
        return true;
    }

    private void AfterInject(Entity<InjectorComponent> injector, EntityUid target)
    {
        // Automatically set syringe to draw after completely draining it.
        if (SolutionContainers.TryGetSolution(injector.Owner, injector.Comp.SolutionName, out _,
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
        if (SolutionContainers.TryGetSolution(injector.Owner, injector.Comp.SolutionName, out _,
                out var solution) && solution.AvailableVolume == 0)
        {
            SetMode(injector, InjectorToggleMode.Inject);
        }

        // Leave some DNA from the drawee on it
        var ev = new TransferDnaEvent { Donor = target, Recipient = injector };
        RaiseLocalEvent(target, ref ev);
    }

    private bool TryDraw(Entity<InjectorComponent> injector, Entity<BloodstreamComponent?> target,
        Entity<SolutionComponent> targetSolution, EntityUid user)
    {
        if (!SolutionContainers.TryGetSolution(injector.Owner, injector.Comp.SolutionName, out var soln,
                out var solution) || solution.AvailableVolume == 0)
        {
            return false;
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
            return false;
        }

        // We have some snowflaked behavior for streams.
        if (target.Comp != null)
        {
            DrawFromBlood(injector, (target.Owner, target.Comp), soln.Value, realTransferAmount, user);
            return true;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = SolutionContainers.Draw(target.Owner, targetSolution, realTransferAmount);

        if (!SolutionContainers.TryAddSolution(soln.Value, removedSolution))
        {
            return false;
        }

        Popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
            ("amount", removedSolution.Volume),
            ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);

        Dirty(injector);
        AfterDraw(injector, target);
        return true;
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
