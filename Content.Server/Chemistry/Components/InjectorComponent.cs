using System;
using System.Threading.Tasks;
using Content.Server.Body.Circulatory;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Notification.Managers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
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
        /// <summary>
        /// Whether or not the injector is able to draw from containers or if it's a single use
        /// device that can only inject.
        /// </summary>
        [ViewVariables] [DataField("injectOnly")]
        private bool _injectOnly;

        /// <summary>
        /// Amount to inject or draw on each usage. If the injector is inject only, it will
        /// attempt to inject it's entire contents upon use.
        /// </summary>
        [ViewVariables] [DataField("transferAmount")]
        private ReagentUnit _transferAmount = ReagentUnit.New(5);

        /// <summary>
        /// Initial storage volume of the injector
        /// </summary>
        [ViewVariables] [DataField("initialMaxVolume")]
        private ReagentUnit _initialMaxVolume = ReagentUnit.New(15);

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
            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
                return false;

            var solutionsSys = EntitySystem.Get<SolutionContainerSystem>();
            //Make sure we have the attacking entity
            if (eventArgs.Target == null || !solutionsSys.HasSolution(Owner))
            {
                return false;
            }

            var targetEntity = eventArgs.Target;


            // Handle injecting/drawing for solutions
            if (solutionsSys.TryGetInjectableSolution(targetEntity, out var injectableSolution))
            {
                if (ToggleState == InjectorToggleMode.Inject)
                {
                    TryInject(injectableSolution, eventArgs.User);
                }
                else
                {
                    eventArgs.User.PopupMessage(eventArgs.User,
                        Loc.GetString("injector-component-cannot-transfer-message",
                            ("owner", injectableSolution.Owner)));
                }
            }
            else if (solutionsSys.TryGetDrawableSolution(targetEntity, out var drawableSolution))
            {
                if (ToggleState == InjectorToggleMode.Draw)
                {
                    TryDraw(drawableSolution, eventArgs.User);
                }
                else
                {
                    eventArgs.User.PopupMessage(eventArgs.User,
                        Loc.GetString("injector-component-cannot-draw-message",
                            ("owner", drawableSolution.Owner)));
                }
            }

            // Handle injecting into bloodstream
            else if (targetEntity.TryGetComponent(out BloodstreamComponent? bloodstream) &&
                     ToggleState == InjectorToggleMode.Inject)
            {
                TryInjectIntoBloodstream(bloodstream, eventArgs.User);
            }

            return true;
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
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, "bloodstream", out var solution)
                || solution.CurrentVolume == 0)
                return;

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(_transferAmount, targetBloodstream.EmptyVolume);

            if (realTransferAmount <= 0)
            {
                Owner.PopupMessage(user,
                    Loc.GetString("injector-component-cannot-inject-message", ("owner", targetBloodstream.Owner)));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution =
                EntitySystem.Get<SolutionContainerSystem>().SplitSolution(solution, realTransferAmount);

            if (!solution.CanAddSolution(removedSolution))
            {
                return;
            }

            // TODO: Account for partial transfer.

            removedSolution.DoEntityReaction(solution.Owner, ReactionMethod.Injection);

            EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(solution, removedSolution);

            removedSolution.DoEntityReaction(targetBloodstream.Owner, ReactionMethod.Injection);

            Owner.PopupMessage(user,
                Loc.GetString("injector-component-inject-success-message",
                    ("amount", removedSolution.TotalVolume),
                    ("target", targetBloodstream.Owner)));
            Dirty();
            AfterInject();
        }

        private void TryInject(Solution targetSolution, IEntity user)
        {
            if (!Owner.TryGetComponent(out Solution? solution) || solution.CurrentVolume == 0)
            {
                return;
            }

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(_transferAmount, targetSolution.InjectSpaceAvailable);

            if (realTransferAmount <= 0)
            {
                Owner.PopupMessage(user,
                    Loc.GetString("injector-component-target-already-full-message", ("target", targetSolution.Owner)));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = EntitySystem.Get<SolutionContainerSystem>()
                .SplitSolution(solution, realTransferAmount);

            removedSolution.DoEntityReaction(targetSolution.Owner, ReactionMethod.Injection);

            EntitySystem.Get<SolutionContainerSystem>()
                .Inject(targetSolution, removedSolution);

            Owner.PopupMessage(user,
                Loc.GetString("injector-component-transfer-success-message",
                    ("amount", removedSolution.TotalVolume),
                    ("target", targetSolution.Owner)));
            Dirty();
            AfterInject();
        }

        private void AfterInject()
        {
            // Automatically set syringe to draw after completely draining it.
            if (EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, "injector", out var solution)
                && solution.CurrentVolume == 0)
            {
                ToggleState = InjectorToggleMode.Draw;
            }
        }

        private void AfterDraw()
        {
            // Automatically set syringe to inject after completely filling it.
            if (EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, "injector", out var solution)
                && solution.EmptyVolume == 0)
            {
                ToggleState = InjectorToggleMode.Inject;
            }
        }

        private void TryDraw(Solution targetSolution, IEntity user)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, "injector", out var solution)
            || solution.EmptyVolume == 0)
            {
                return;
            }

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(_transferAmount, targetSolution.DrawAvailable);

            if (realTransferAmount <= 0)
            {
                Owner.PopupMessage(user,
                    Loc.GetString("injector-component-target-is-empty-message", ("target", targetSolution.Owner)));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = EntitySystem.Get<SolutionContainerSystem>()
                .Draw(targetSolution, realTransferAmount);

            if (!EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(solution, removedSolution))
            {
                return;
            }

            Owner.PopupMessage(user,
                Loc.GetString("injector-component-draw-success-message",
                    ("amount", removedSolution.TotalVolume),
                    ("target", targetSolution.Owner)));
            Dirty();
            AfterDraw();
        }



        public override ComponentState GetComponentState(ICommonSession player)
        {
            Owner.EntityManager.EntitySysManager.GetEntitySystem<SolutionContainerSystem>()
                .TryGetSolution(Owner, "injector", out var solution);

            var currentVolume = solution?.CurrentVolume ?? ReagentUnit.Zero;
            var maxVolume = solution?.MaxVolume ?? ReagentUnit.Zero;

            return new InjectorComponentState(currentVolume, maxVolume, ToggleState);
        }
    }
}
