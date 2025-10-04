using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
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
using Content.Shared.Verbs;

namespace Content.Shared.Chemistry.EntitySystems;

public abstract class SharedInjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _blood = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedForensicsSystem _forensics = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainer = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InjectorComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
        SubscribeLocalEvent<InjectorComponent, UseInHandEvent>(OnInjectorUse);
        SubscribeLocalEvent<InjectorComponent, AfterInteractEvent>(OnInjectorAfterInteract);
        SubscribeLocalEvent<InjectorComponent, InjectorDoAfterEvent>(OnInjectDoAfter);
    }

    private void AddSetTransferVerbs(Entity<InjectorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (ent.Comp.TransferAmounts.Count <= 1)
            return; // No options to cycle between

        var user = args.User;

        var min = ent.Comp.TransferAmounts.Min();
        var max = ent.Comp.TransferAmounts.Max();
        var cur = ent.Comp.CurrentTransferAmount;
        var toggleAmount = cur == max ? min : max;

        var priority = 0;
        AlternativeVerb toggleVerb = new()
        {
            Text = Loc.GetString("comp-solution-transfer-verb-toggle", ("amount", toggleAmount)),
            Category = VerbCategory.SetTransferAmount,
            Act = () =>
            {
                ent.Comp.CurrentTransferAmount = toggleAmount;
                _popup.PopupClient(Loc.GetString("comp-solution-transfer-set-amount", ("amount", toggleAmount)), user, user);
                Dirty(ent);
            },

            Priority = priority
        };
        args.Verbs.Add(toggleVerb);

        priority -= 1;

        // Add specific transfer verbs for amounts defined in the component
        foreach (var amount in ent.Comp.TransferAmounts)
        {
            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount)),
                Category = VerbCategory.SetTransferAmount,
                Act = () =>
                {
                    ent.Comp.CurrentTransferAmount = amount;
                    _popup.PopupClient(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)), user, user);
                    Dirty(ent);
                },

                // we want to sort by size, not alphabetically by the verb text.
                Priority = priority
            };

            priority -= 1;

            args.Verbs.Add(verb);
        }
    }

    private void OnInjectorUse(Entity<InjectorComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Toggle(ent, args.User);
        args.Handled = true;
    }

    private void OnInjectorAfterInteract(Entity<InjectorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        //Make sure we have the attacking entity
        if (args.Target is not { Valid: true } target || !HasComp<SolutionContainerManagerComponent>(ent))
            return;

        // Is the target a mob? If yes, use a do-after to give them time to respond.
        if (HasComp<MobStateComponent>(target) || HasComp<BloodstreamComponent>(target))
        {
            // Are use using an injector capable of targeting a mob?
            if (ent.Comp.IgnoreMobs)
                return;

            InjectDoAfter(ent, target, args.User);
            args.Handled = true;
            return;
        }

        // Instantly draw from or inject into jugs, bottles etc.
        args.Handled = TryUseInjector(ent, target, args.User);
    }

    private void OnInjectDoAfter(Entity<InjectorComponent> ent, ref InjectorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        args.Handled = TryUseInjector(ent, args.Args.Target.Value, args.Args.User);
    }

    /// <summary>
    /// Send informative pop-up messages and wait for a do-after to complete.
    /// </summary>
    private void InjectDoAfter(Entity<InjectorComponent> injector, EntityUid target, EntityUid user)
    {
        // Create a pop-up for the user
        if (injector.Comp.ToggleState == InjectorToggleMode.Draw)
        {
            _popup.PopupClient(Loc.GetString("injector-component-drawing-user"), target, user);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("injector-component-injecting-user"), target, user);
        }

        if (!SolutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution, out var solution))
            return;

        var actualDelay = injector.Comp.Delay;
        FixedPoint2 amountToInject;
        if (injector.Comp.ToggleState == InjectorToggleMode.Draw)
        {
            // additional delay is based on actual volume left to draw in syringe when smaller than transfer amount
            amountToInject = FixedPoint2.Min(injector.Comp.CurrentTransferAmount, solution.MaxVolume - solution.Volume);
        }
        else
        {
            // additional delay is based on actual volume left to inject in syringe when smaller than transfer amount
            amountToInject = FixedPoint2.Min(injector.Comp.CurrentTransferAmount, solution.Volume);
        }

        // Injections take 0.5 seconds longer per 5u of possible space/content
        // First 5u(MinimumTransferAmount) doesn't incur delay
        actualDelay += injector.Comp.DelayPerVolume * FixedPoint2.Max(0, amountToInject - injector.Comp.TransferAmounts.Min()).Double();

        // Ensure that minimum delay before incapacitation checks is 1 seconds
        actualDelay = MathHelper.Max(actualDelay, TimeSpan.FromSeconds(1));

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
                _popup.PopupEntity(Loc.GetString("injector-component-injecting-target",
    ("user", userName)), user, target);
            }


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
            if (injector.Comp.ToggleState == InjectorToggleMode.Inject)
            {
                _adminLogger.Add(LogType.ForceFeed,
                    $"{ToPrettyString(user):user} is attempting to inject {ToPrettyString(target):target} with a solution {SharedSolutionContainerSystem.ToPrettyString(solution):solution}");
            }
            else
            {
                _adminLogger.Add(LogType.ForceFeed,
                    $"{ToPrettyString(user):user} is attempting to draw {injector.Comp.CurrentTransferAmount.ToString()} units from {ToPrettyString(target):target}");
            }
        }
        else // injecting yourself
        {
            // Self-injections take half as long.
            actualDelay /= 2;

            if (injector.Comp.ToggleState == InjectorToggleMode.Inject)
            {
                _adminLogger.Add(LogType.Ingestion,
                    $"{ToPrettyString(user):user} is attempting to inject themselves with a solution {SharedSolutionContainerSystem.ToPrettyString(solution):solution}.");
            }
            else
            {
                _adminLogger.Add(LogType.ForceFeed,
                    $"{ToPrettyString(user):user} is attempting to draw {injector.Comp.CurrentTransferAmount.ToString()} units from themselves.");
            }
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, actualDelay, new InjectorDoAfterEvent(), injector.Owner, target: target, used: injector.Owner)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = injector.Comp.NeedHand,
            BreakOnHandChange = injector.Comp.BreakOnHandChange,
            MovementThreshold = injector.Comp.MovementThreshold,
        });
    }

    private bool TryUseInjector(Entity<InjectorComponent> injector, EntityUid target, EntityUid user)
    {
        var isOpenOrIgnored = injector.Comp.IgnoreClosed || !_openable.IsClosed(target);
        // Handle injecting/drawing for solutions
        if (injector.Comp.ToggleState == InjectorToggleMode.Inject)
        {
            if (isOpenOrIgnored && SolutionContainer.TryGetInjectableSolution(target, out var injectableSolution, out _))
                return TryInject(injector, target, injectableSolution.Value, user, false);

            if (isOpenOrIgnored && SolutionContainer.TryGetRefillableSolution(target, out var refillableSolution, out _))
                return TryInject(injector, target, refillableSolution.Value, user, true);

            if (TryComp<BloodstreamComponent>(target, out var bloodstream))
                return TryInjectIntoBloodstream(injector, (target, bloodstream), user);

            LocId msg = target == user ? "injector-component-cannot-transfer-message-self" : "injector-component-cannot-transfer-message";
            _popup.PopupClient(Loc.GetString(msg, ("target", Identity.Entity(target, EntityManager))), injector, user);
        }
        else if (injector.Comp.ToggleState == InjectorToggleMode.Draw)
        {
            // Draw from a bloodstream, if the target has that
            if (TryComp<BloodstreamComponent>(target, out var stream) &&
                SolutionContainer.ResolveSolution(target, stream.BloodSolutionName, ref stream.BloodSolution))
            {
                return TryDraw(injector, (target, stream), stream.BloodSolution.Value, user);
            }

            // Draw from an object (food, beaker, etc)
            if (isOpenOrIgnored && SolutionContainer.TryGetDrawableSolution(target, out var drawableSolution, out _))
                return TryDraw(injector, target, drawableSolution.Value, user);

            LocId msg = target == user ? "injector-component-cannot-draw-message-self" : "injector-component-cannot-draw-message";
            _popup.PopupClient(Loc.GetString(msg, ("target", Identity.Entity(target, EntityManager))), injector.Owner, user);
        }
        return false;
    }

    private bool TryInject(Entity<InjectorComponent> injector, EntityUid target,
        Entity<SolutionComponent> targetSolution, EntityUid user, bool asRefill)
    {
        if (!SolutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution,
                out var solution) || solution.Volume == 0)
            return false;

        // Get transfer amount. May be smaller than _transferAmount if not enough room
        var realTransferAmount =
            FixedPoint2.Min(injector.Comp.CurrentTransferAmount, targetSolution.Comp.Solution.AvailableVolume);

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
            removedSolution = SolutionContainer.SplitStackSolution(injector.Comp.Solution.Value, realTransferAmount, stack.Count);
        else
            removedSolution = SolutionContainer.SplitSolution(injector.Comp.Solution.Value, realTransferAmount);

        _reactiveSystem.DoEntityReaction(target, removedSolution, ReactionMethod.Injection);

        if (!asRefill)
            SolutionContainer.Inject(target, targetSolution, removedSolution);
        else
            SolutionContainer.Refill(target, targetSolution, removedSolution);

        LocId msgSuccess = target == user ? "injector-component-transfer-success-message-self" : "injector-component-transfer-success-message";
        _popup.PopupClient(
            Loc.GetString(msgSuccess,
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(target, EntityManager))),
            injector.Owner, user);

        AfterInject(injector, target);
        return true;
    }

    private bool TryInjectIntoBloodstream(Entity<InjectorComponent> injector, Entity<BloodstreamComponent> target,
        EntityUid user)
    {
        // Get transfer amount. May be smaller than _transferAmount if not enough room
        if (!SolutionContainer.ResolveSolution(target.Owner, target.Comp.ChemicalSolutionName,
                ref target.Comp.ChemicalSolution, out var chemSolution))
        {
            LocId msg = target.Owner == user ? "injector-component-cannot-inject-message-self" : "injector-component-cannot-inject-message";
            _popup.PopupClient(
                Loc.GetString(msg,
                    ("target", Identity.Entity(target, EntityManager))),
                injector.Owner, user);
            return false;
        }

        var realTransferAmount = FixedPoint2.Min(injector.Comp.CurrentTransferAmount, chemSolution.AvailableVolume);
        if (realTransferAmount <= 0)
        {
            LocId msg = target.Owner == user ? "injector-component-cannot-inject-message-self" : "injector-component-cannot-inject-message";
            _popup.PopupClient(
                Loc.GetString(msg,
                    ("target", Identity.Entity(target, EntityManager))),
                injector.Owner, user);
            return false;
        }

        // Move units from attackSolution to targetSolution
        var removedSolution = SolutionContainer.SplitSolution(target.Comp.ChemicalSolution.Value, realTransferAmount);

        _blood.TryAddToChemicals(target.AsNullable(), removedSolution);

        _reactiveSystem.DoEntityReaction(target, removedSolution, ReactionMethod.Injection);

        LocId msgSuccess = target.Owner == user ? "injector-component-inject-success-message-self" : "injector-component-inject-success-message";
        _popup.PopupClient(
            Loc.GetString(msgSuccess,
                ("amount", removedSolution.Volume),
                ("target", Identity.Entity(target, EntityManager))),
            injector.Owner, user);

        AfterInject(injector, target);
        return true;
    }

    private bool TryDraw(Entity<InjectorComponent> injector, Entity<BloodstreamComponent?> target,
        Entity<SolutionComponent> targetSolution, EntityUid user)
    {
        if (!SolutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution,
                out var solution) || solution.AvailableVolume == 0)
        {
            return false;
        }

        var applicableTargetSolution = targetSolution.Comp.Solution;
        // If a whitelist exists, remove all non-whitelisted reagents from the target solution temporarily
        var temporarilyRemovedSolution = new Solution();
        if (injector.Comp.ReagentWhitelist is { } reagentWhitelist)
        {
            temporarilyRemovedSolution = applicableTargetSolution.SplitSolutionWithout(applicableTargetSolution.Volume, reagentWhitelist.ToArray());
        }

        // Get transfer amount. May be smaller than _transferAmount if not enough room, also make sure there's room in the injector
        var realTransferAmount = FixedPoint2.Min(injector.Comp.CurrentTransferAmount, applicableTargetSolution.Volume,
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
        var removedSolution = SolutionContainer.Draw(target.Owner, targetSolution, realTransferAmount);

        // Add back non-whitelisted reagents to the target solution
        SolutionContainer.TryAddSolution(targetSolution, temporarilyRemovedSolution);

        if (!SolutionContainer.TryAddSolution(injector.Comp.Solution.Value, removedSolution))
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

        if (SolutionContainer.ResolveSolution(target.Owner, target.Comp.ChemicalSolutionName,
                ref target.Comp.ChemicalSolution))
        {
            var chemTemp = SolutionContainer.SplitSolution(target.Comp.ChemicalSolution.Value, drawAmount * 0.15f);
            SolutionContainer.TryAddSolution(injectorSolution, chemTemp);
            drawAmount -= (float)chemTemp.Volume;
        }

        if (SolutionContainer.ResolveSolution(target.Owner, target.Comp.BloodSolutionName,
                ref target.Comp.BloodSolution))
        {
            var bloodTemp = SolutionContainer.SplitSolution(target.Comp.BloodSolution.Value, drawAmount);
            SolutionContainer.TryAddSolution(injectorSolution, bloodTemp);
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
        // Automatically set syringe to draw after completely draining it.
        if (SolutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution,
                out var solution) && solution.Volume == 0)
        {
            SetMode(injector, InjectorToggleMode.Draw);
        }

        // Leave some DNA from the injectee on it
        _forensics.TransferDna(injector, target);
    }

    private void AfterDraw(Entity<InjectorComponent> injector, EntityUid target)
    {
        // Automatically set syringe to inject after completely filling it.
        if (SolutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution,
                out var solution) && solution.AvailableVolume == 0)
        {
            SetMode(injector, InjectorToggleMode.Inject);
        }

        // Leave some DNA from the drawee on it
        _forensics.TransferDna(injector, target);
    }

    /// <summary>
    /// Toggle the injector between draw/inject state if applicable.
    /// </summary>
    public void Toggle(Entity<InjectorComponent> injector, EntityUid user)
    {
        if (injector.Comp.InjectOnly)
            return;

        if (!SolutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution, out var solution))
            return;

        string msg;

        switch (injector.Comp.ToggleState)
        {
            case InjectorToggleMode.Inject:
                if (solution.AvailableVolume > 0) // If solution has empty space to fill up, allow toggling to draw
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
                if (solution.Volume > 0) // If solution has anything in it, allow toggling to inject
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
    /// Set the mode of the injector to draw or inject.
    /// </summary>
    public void SetMode(Entity<InjectorComponent> injector, InjectorToggleMode mode)
    {
        injector.Comp.ToggleState = mode;
        Dirty(injector);
    }
}
