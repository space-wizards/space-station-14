using System;
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
        ///  The base delay has a minimum of 1 second, but this will still be modified if the target is incapcatiated or
        ///  in combat mode.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("delay")]
        public float Delay = 5;

        /// <summary>
        /// Is this component currently being used in a DoAfter?
        /// </summary>
        public bool InUse = false;

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
        private void Toggle(IEntity user)
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
            if (InUse)
                return false;

            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
                return false;

            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(eventArgs.User.Uid))
                return false;

            var solutionsSys = EntitySystem.Get<SolutionContainerSystem>();
            //Make sure we have the attacking entity
            if (eventArgs.Target == null || !Owner.HasComponent<SolutionContainerManagerComponent>())
            {
                return false;
            }

            var targetEntity = eventArgs.Target;

            // is the target a mob? If yes, add a use delay
            if (Owner.EntityManager.HasComponent<MobStateComponent>(targetEntity.Uid) ||
                Owner.EntityManager.HasComponent<BloodstreamComponent>(targetEntity.Uid))
            {
                if (!await TryInjectDoAfter(eventArgs.User.Uid, eventArgs.Target.Uid))
                    return true;
            }

            // Handle injecting/drawing for solutions
            if (ToggleState == InjectorToggleMode.Inject)
            {
                if (solutionsSys.TryGetInjectableSolution(targetEntity.Uid, out var injectableSolution))
                {
                    TryInject(targetEntity, injectableSolution, eventArgs.User, false);
                }
                else if (solutionsSys.TryGetRefillableSolution(targetEntity.Uid, out var refillableSolution))
                {
                    TryInject(targetEntity, refillableSolution, eventArgs.User, true);
                }
                else if (targetEntity.TryGetComponent(out BloodstreamComponent? bloodstream))
                {
                    TryInjectIntoBloodstream(bloodstream, eventArgs.User);
                }
                else
                {
                    eventArgs.User.PopupMessage(eventArgs.User,
                        Loc.GetString("injector-component-cannot-transfer-message",
                            ("target", targetEntity)));
                }
            }
            else if (ToggleState == InjectorToggleMode.Draw)
            {
                if (solutionsSys.TryGetDrawableSolution(targetEntity.Uid, out var drawableSolution))
                {
                    TryDraw(targetEntity, drawableSolution, eventArgs.User);
                }
                else
                {
                    eventArgs.User.PopupMessage(eventArgs.User,
                        Loc.GetString("injector-component-cannot-draw-message",
                            ("target", targetEntity)));
                }
            }

            return true;
        }

        /// <summary>
        /// Send informative pop-up messages and wait for a Do-After to complete.
        /// </summary>
        public async Task<bool> TryInjectDoAfter(EntityUid user, EntityUid target)
        {
            InUse = true;
            var popupSys = EntitySystem.Get<SharedPopupSystem>();

            // pop-up for the user
            popupSys.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, Filter.Entities(user));

            // get entity for logging. Log with EntityUids when?
            var userEntity = Owner.EntityManager.GetEntity(user);
            var logSys = EntitySystem.Get<AdminLogSystem>();

            var actualDelay = MathF.Max(Delay, 1f);
            if (user != target)
            {
                // pop-up for the target
                var userName = Owner.EntityManager.GetComponent<MetaDataComponent>(user).EntityName;
                popupSys.PopupEntity(Loc.GetString("injector-component-injecting-target",
                    ("user", userName)), user, Filter.Entities(target));

                // check if the target is incapacitated or in combat mode and modify time accordingly.
                if (Owner.EntityManager.TryGetComponent<MobStateComponent>(target, out var mobState) &&
                    mobState.IsIncapacitated())
                {
                    actualDelay /= 2;
                }
                else if (Owner.EntityManager.TryGetComponent<CombatModeComponent>(target, out var combat) &&
                    combat.IsInCombatMode)
                {
                    // Slight delay increase when target is in combat mode delay.
                    // Helps prevents cheese injection in combat with fast syringes & lag.
                    actualDelay += 1;
                }

                // Lets log this. Using the "force feed" log type. It's not quite feeding, but the effect is the same.
                var targetEntity = Owner.EntityManager.GetEntity(target);
                if (ToggleState == InjectorToggleMode.Inject)
                {
                    logSys.Add(LogType.ForceFeed,
                        $"{userEntity} is attempting to injecting a solution into {targetEntity}");
                    // TODO solution pretty string.
                }
            }
            else
            {
                // act twice as fast for self-injections.
                actualDelay /= 2;

                // and lets log it. TODO solution pretty string.
                if (ToggleState == InjectorToggleMode.Inject)
                    logSys.Add(LogType.Ingestion,
                        $"{userEntity} is attempting to injecting themselves with a solution.");
            }

            var status = await EntitySystem.Get<DoAfterSystem>().WaitDoAfter(
                new DoAfterEventArgs(user, actualDelay, target: target)
                {
                    BreakOnUserMove = true,
                    BreakOnDamage = true,
                    BreakOnStun = true,
                    BreakOnTargetMove = true,
                    MovementThreshold = 1.0f
                });
            InUse = false;

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

        private void TryInjectIntoBloodstream(BloodstreamComponent targetBloodstream, IEntity user)
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
                EntitySystem.Get<SolutionContainerSystem>().SplitSolution(user.Uid, targetBloodstream.Solution, realTransferAmount);

            var bloodstreamSys = EntitySystem.Get<BloodstreamSystem>();
            bloodstreamSys.TryAddToBloodstream(targetBloodstream.OwnerUid, removedSolution, targetBloodstream);

            removedSolution.DoEntityReaction(targetBloodstream.Owner.Uid, ReactionMethod.Injection);

            Owner.PopupMessage(user,
                Loc.GetString("injector-component-inject-success-message",
                    ("amount", removedSolution.TotalVolume),
                    ("target", targetBloodstream.Owner)));
            Dirty();
            AfterInject();
        }

        private void TryInject(IEntity targetEntity, Solution targetSolution, IEntity user, bool asRefill)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution)
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
            var removedSolution = EntitySystem.Get<SolutionContainerSystem>().SplitSolution(Owner.Uid, solution, realTransferAmount);

            removedSolution.DoEntityReaction(targetEntity.Uid, ReactionMethod.Injection);

            if (!asRefill)
            {
                EntitySystem.Get<SolutionContainerSystem>()
                    .Inject(targetEntity.Uid, targetSolution, removedSolution);
            }
            else
            {
                EntitySystem.Get<SolutionContainerSystem>()
                    .Refill(targetEntity.Uid, targetSolution, removedSolution);
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
            if (EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution)
                && solution.CurrentVolume == 0)
            {
                ToggleState = InjectorToggleMode.Draw;
            }
        }

        private void AfterDraw()
        {
            // Automatically set syringe to inject after completely filling it.
            if (EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution)
                && solution.AvailableVolume == 0)
            {
                ToggleState = InjectorToggleMode.Inject;
            }
        }

        private void TryDraw(IEntity targetEntity, Solution targetSolution, IEntity user)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner.Uid, SolutionName, out var solution)
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
                .Draw(targetEntity.Uid, targetSolution, realTransferAmount);

            if (!EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(targetEntity.Uid, solution, removedSolution))
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
            Owner.EntityManager.EntitySysManager.GetEntitySystem<SolutionContainerSystem>()
                .TryGetSolution(Owner.Uid, SolutionName, out var solution);

            var currentVolume = solution?.CurrentVolume ?? FixedPoint2.Zero;
            var maxVolume = solution?.MaxVolume ?? FixedPoint2.Zero;

            return new InjectorComponentState(currentVolume, maxVolume, ToggleState);
        }
    }
}
