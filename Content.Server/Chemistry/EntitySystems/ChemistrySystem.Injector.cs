using Content.Server.Body.Components;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;

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

        args.State = new InjectorComponentState(currentVolume, maxVolume, component.ToggleState);
    }

    private void OnInjectorAfterInteract(EntityUid uid, InjectorComponent component, AfterInteractEvent args)
    {
        if (CancelToken != null)
        {
            CancelToken.Cancel();
            return true;
        }

        if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            return false;

        if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(eventArgs.User))
            return false;

        var solutionsSys = EntitySystem.Get<SolutionContainerSystem>();
        //Make sure we have the attacking entity
        if (eventArgs.Target is not {Valid: true} target ||
            !_entities.HasComponent<SolutionContainerManagerComponent>(Owner))
        {
            return false;
        }

        // Is the target a mob? If yes, use a do-after to give them time to respond.
        if (_entities.HasComponent<MobStateComponent>(target) ||
            _entities.HasComponent<BloodstreamComponent>(target))
        {
            if (!await TryInjectDoAfter(eventArgs.User, target))
                return true;
        }

        // Handle injecting/drawing for solutions
        if (ToggleState == SharedInjectorComponent.InjectorToggleMode.Inject)
        {
            if (solutionsSys.TryGetInjectableSolution(target, out var injectableSolution))
            {
                TryInject(target, injectableSolution, eventArgs.User, false);
            }
            else if (solutionsSys.TryGetRefillableSolution(target, out var refillableSolution))
            {
                TryInject(target, refillableSolution, eventArgs.User, true);
            }
            else if (_entities.TryGetComponent(target, out BloodstreamComponent? bloodstream))
            {
                TryInjectIntoBloodstream(bloodstream, eventArgs.User);
            }
            else
            {
                eventArgs.User.PopupMessage(eventArgs.User,
                    Loc.GetString("injector-component-cannot-transfer-message",
                        ("target", target)));
            }
        }
        else if (ToggleState == SharedInjectorComponent.InjectorToggleMode.Draw)
        {
            if (solutionsSys.TryGetDrawableSolution(target, out var drawableSolution))
            {
                TryDraw(target, drawableSolution, eventArgs.User);
            }
            else
            {
                eventArgs.User.PopupMessage(eventArgs.User,
                    Loc.GetString("injector-component-cannot-draw-message",
                        ("target", target)));
            }
        }

        return true;
    }

    private void OnInjectorStartup(EntityUid uid, InjectorComponent component, ComponentStartup args)
    {
        Dirty(component);
    }

    private void OnInjectorUse(EntityUid uid, InjectorComponent component, UseInHandEvent args)
    {
        Toggle(eventArgs.User);
        return true;
    }

            /// <summary>
        /// Toggle between draw/inject state if applicable
        /// </summary>
        private void Toggle(EntityUid user)
        {
            if (_injectOnly)
            {
                return;
            }

            string msg;
            switch (ToggleState)
            {
                case InjectorToggleMode.Inject:
                    ToggleState = InjectorToggleMode.Draw;
                    msg = "injector-component-drawing-text";
                    break;
                case InjectorToggleMode.Draw:
                    ToggleState = InjectorToggleMode.Inject;
                    msg = "injector-component-injecting-text";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Owner.PopupMessage(user, Loc.GetString(msg));
        }

        /// <summary>
        /// Send informative pop-up messages and wait for a do-after to complete.
        /// </summary>
        public async Task<bool> TryInjectDoAfter(EntityUid user, EntityUid target)
        {
            var popupSys = EntitySystem.Get<SharedPopupSystem>();

            // Create a pop-up for the user
            popupSys.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, Filter.Entities(user));

            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
                return false;

            // Get entity for logging. Log with EntityUids when?
            var logSys = EntitySystem.Get<AdminLogSystem>();

            var actualDelay = MathF.Max(Delay, 1f);
            if (user != target)
            {
                // Create a pop-up for the target
                var userName = _entities.GetComponent<MetaDataComponent>(user).EntityName;
                popupSys.PopupEntity(Loc.GetString("injector-component-injecting-target",
                    ("user", userName)), user, Filter.Entities(target));

                // Check if the target is incapacitated or in combat mode and modify time accordingly.
                if (_entities.TryGetComponent<MobStateComponent>(target, out var mobState) &&
                    mobState.IsIncapacitated())
                {
                    actualDelay /= 2;
                }
                else if (_entities.TryGetComponent<CombatModeComponent>(target, out var combat) &&
                         combat.IsInCombatMode)
                {
                    // Slightly increase the delay when the target is in combat mode. Helps prevents cheese injections in
                    // combat with fast syringes & lag.
                    actualDelay += 1;
                }

                // Add an admin log, using the "force feed" log type. It's not quite feeding, but the effect is the same.
                if (ToggleState == InjectorToggleMode.Inject)
                {
                    logSys.Add(LogType.ForceFeed,
                        $"{_entities.ToPrettyString(user):user} is attempting to inject {_entities.ToPrettyString(target):target} with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}");
                    // TODO solution pretty string.
                }
            }
            else
            {
                // Self-injections take half as long.
                actualDelay /= 2;

                if (ToggleState == InjectorToggleMode.Inject)
                    logSys.Add(LogType.Ingestion,
                        $"{_entities.ToPrettyString(user):user} is attempting to inject themselves with a solution {SolutionContainerSystem.ToPrettyString(solution):solution}.");
                    //TODO solution pretty string.
            }

            CancelToken = new();
            var status = await EntitySystem.Get<DoAfterSystem>().WaitDoAfter(
                new DoAfterEventArgs(user, actualDelay, CancelToken.Token, target)
                {
                    BreakOnUserMove = true,
                    BreakOnDamage = true,
                    BreakOnStun = true,
                    BreakOnTargetMove = true,
                    MovementThreshold = 1.0f
                });
            CancelToken = null;

            return status == DoAfterStatus.Finished;
        }

        private void TryInjectIntoBloodstream(BloodstreamComponent targetBloodstream, EntityUid user)
        {
            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = FixedPoint2.Min(_transferAmount, targetBloodstream.Solution.AvailableVolume);

            if (realTransferAmount <= 0)
            {
                Owner.PopupMessage(user,
                    Loc.GetString("injector-component-cannot-inject-message", ("target", targetBloodstream.Owner)));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution =
                EntitySystem.Get<SolutionContainerSystem>().SplitSolution(user, targetBloodstream.Solution, realTransferAmount);

            var bloodstreamSys = EntitySystem.Get<BloodstreamSystem>();
            bloodstreamSys.TryAddToBloodstream((targetBloodstream).Owner, removedSolution, targetBloodstream);

            removedSolution.DoEntityReaction(targetBloodstream.Owner, ReactionMethod.Injection);

            Owner.PopupMessage(user,
                Loc.GetString("injector-component-inject-success-message",
                    ("amount", removedSolution.TotalVolume),
                    ("target", targetBloodstream.Owner)));
            Dirty();
            AfterInject();
        }

        private void TryInject(EntityUid targetEntity, Solution targetSolution, EntityUid user, bool asRefill)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution)
                || solution.CurrentVolume == 0)
            {
                return;
            }

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = FixedPoint2.Min(_transferAmount, targetSolution.AvailableVolume);

            if (realTransferAmount <= 0)
            {
                Owner.PopupMessage(user,
                    Loc.GetString("injector-component-target-already-full-message", ("target", targetEntity)));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = EntitySystem.Get<SolutionContainerSystem>().SplitSolution(Owner, solution, realTransferAmount);

            removedSolution.DoEntityReaction(targetEntity, ReactionMethod.Injection);

            if (!asRefill)
            {
                EntitySystem.Get<SolutionContainerSystem>()
                    .Inject(targetEntity, targetSolution, removedSolution);
            }
            else
            {
                EntitySystem.Get<SolutionContainerSystem>()
                    .Refill(targetEntity, targetSolution, removedSolution);
            }

            Owner.PopupMessage(user,
                Loc.GetString("injector-component-transfer-success-message",
                    ("amount", removedSolution.TotalVolume),
                    ("target", targetEntity)));
            Dirty();
            AfterInject();
        }

        private void AfterInject()
        {
            // Automatically set syringe to draw after completely draining it.
            if (EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution)
                && solution.CurrentVolume == 0)
            {
                ToggleState = InjectorToggleMode.Draw;
            }
        }

        private void AfterDraw()
        {
            // Automatically set syringe to inject after completely filling it.
            if (EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution)
                && solution.AvailableVolume == 0)
            {
                ToggleState = InjectorToggleMode.Inject;
            }
        }

        private void TryDraw(EntityUid targetEntity, Solution targetSolution, EntityUid user)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution)
                || solution.AvailableVolume == 0)
            {
                return;
            }

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = FixedPoint2.Min(_transferAmount, targetSolution.DrawAvailable);

            if (realTransferAmount <= 0)
            {
                Owner.PopupMessage(user,
                    Loc.GetString("injector-component-target-is-empty-message", ("target", targetEntity)));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = EntitySystem.Get<SolutionContainerSystem>()
                .Draw(targetEntity, targetSolution, realTransferAmount);

            if (!EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(targetEntity, solution, removedSolution))
            {
                return;
            }

            Owner.PopupMessage(user,
                Loc.GetString("injector-component-draw-success-message",
                    ("amount", removedSolution.TotalVolume),
                    ("target", targetEntity)));
            Dirty();
            AfterDraw();
        }
}
