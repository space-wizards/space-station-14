using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.CombatMode;
using Content.Server.DoAfter;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Server behavior for reagent injectors and syringes. Can optionally support both
    /// injection and drawing or just injection. Can inject/draw reagents from solution
    /// containers, and can directly inject into a mobs bloodstream.
    /// </summary>
    [RegisterComponent]
    public class InjectorComponent : SharedInjectorComponent, IAfterInteract, IUse
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public const string SolutionName = "injector";

        /// <summary>
        /// Whether or not the injector is able to draw from containers or if it's a single use
        /// device that can only inject.
        /// </summary>
        [ViewVariables]
        [DataField("injectOnly")]
        private bool _injectOnly;

        /// <summary>
        /// Amount to inject or draw on each usage. If the injector is inject only, it will
        /// attempt to inject it's entire contents upon use.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("transferAmount")]
        private FixedPoint2 _transferAmount = FixedPoint2.New(5);

        /// <summary>
        /// Injection delay (seconds) when the target is a mob.
        /// </summary>
        /// <remarks>
        /// The base delay has a minimum of 1 second, but this will still be modified if the target is incapacitated or
        /// in combat mode.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("delay")]
        public float Delay = 5;

        /// <summary>
        ///     Token for interrupting a do-after action (e.g., injection another player). If not null, implies
        ///     component is currently "in use".
        /// </summary>
        public CancellationTokenSource? CancelToken;

        private InjectorToggleMode _toggleState;

        /// <summary>
        /// The state of the injector. Determines it's attack behavior. Containers must have the
        /// right SolutionCaps to support injection/drawing. For InjectOnly injectors this should
        /// only ever be set to Inject
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public InjectorToggleMode ToggleState
        {
            get => _toggleState;
            set
            {
                _toggleState = value;
                Dirty();
            }
        }

        protected override void Startup()
        {
            base.Startup();

            Dirty();
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
        /// Called when clicking on entities while holding in active hand
        /// </summary>
        /// <param name="eventArgs"></param>
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (CancelToken != null)
            {
                CancelToken.Cancel();
                return true;
            }

            if (!eventArgs.CanReach)
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
            if (ToggleState == InjectorToggleMode.Inject)
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
            else if (ToggleState == InjectorToggleMode.Draw)
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

        /// <summary>
        /// Called when use key is pressed when held in active hand
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Toggle(eventArgs.User);
            return true;
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


        public override ComponentState GetComponentState()
        {
            _entities.EntitySysManager.GetEntitySystem<SolutionContainerSystem>()
                .TryGetSolution(Owner, SolutionName, out var solution);

            var currentVolume = solution?.CurrentVolume ?? FixedPoint2.Zero;
            var maxVolume = solution?.MaxVolume ?? FixedPoint2.Zero;

            return new InjectorComponentState(currentVolume, maxVolume, ToggleState);
        }
    }
}
