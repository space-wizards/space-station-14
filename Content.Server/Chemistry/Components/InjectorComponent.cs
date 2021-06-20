#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.Body.Circulatory;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
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
    public class InjectorComponent : SharedInjectorComponent, IAfterInteract, IUse, ISolutionChange
    {
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
        [ViewVariables]
        [DataField("transferAmount")]
        private ReagentUnit _transferAmount = ReagentUnit.New(5);

        /// <summary>
        /// Initial storage volume of the injector
        /// </summary>
        [ViewVariables]
        [DataField("initialMaxVolume")]
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
                    msg = "Now drawing";
                    break;
                case InjectorToggleMode.Draw:
                    ToggleState = InjectorToggleMode.Inject;
                    msg = "Now injecting";
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

            //Make sure we have the attacking entity
            if (eventArgs.Target == null || !Owner.HasComponent<SolutionContainerComponent>())
            {
                return false;
            }

            var targetEntity = eventArgs.Target;

            // Handle injecting/drawing for solutions
            if (targetEntity.TryGetComponent<ISolutionInteractionsComponent>(out var targetSolution))
            {
                if (ToggleState == InjectorToggleMode.Inject)
                {
                    if (targetSolution.CanInject)
                    {
                        TryInject(targetSolution, eventArgs.User);
                    }
                    else
                    {
                        eventArgs.User.PopupMessage(eventArgs.User,
                            Loc.GetString("You aren't able to transfer to {0:theName}!", targetSolution.Owner));
                    }
                }
                else if (ToggleState == InjectorToggleMode.Draw)
                {
                    if (targetSolution.CanDraw)
                    {
                        TryDraw(targetSolution, eventArgs.User);
                    }
                    else
                    {
                        eventArgs.User.PopupMessage(eventArgs.User,
                            Loc.GetString("You aren't able to draw from {0:theName}!", targetSolution.Owner));
                    }
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
            if (!Owner.TryGetComponent(out SolutionContainerComponent? solution) || solution.CurrentVolume == 0)
            {
                return;
            }

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(_transferAmount, targetBloodstream.EmptyVolume);

            if (realTransferAmount <= 0)
            {
                Owner.PopupMessage(user,
                    Loc.GetString("You aren't able to inject {0:theName}!", targetBloodstream.Owner));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = solution.SplitSolution(realTransferAmount);

            if (!solution.CanAddSolution(removedSolution))
            {
                return;
            }

            // TODO: Account for partial transfer.

            removedSolution.DoEntityReaction(solution.Owner, ReactionMethod.Injection);

            solution.TryAddSolution(removedSolution);

            removedSolution.DoEntityReaction(targetBloodstream.Owner, ReactionMethod.Injection);

            Owner.PopupMessage(user,
                Loc.GetString("You inject {0}u into {1:theName}!", removedSolution.TotalVolume,
                    targetBloodstream.Owner));
            Dirty();
            AfterInject();
        }

        private void TryInject(ISolutionInteractionsComponent targetSolution, IEntity user)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent? solution) || solution.CurrentVolume == 0)
            {
                return;
            }

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(_transferAmount, targetSolution.InjectSpaceAvailable);

            if (realTransferAmount <= 0)
            {
                Owner.PopupMessage(user, Loc.GetString("{0:theName} is already full!", targetSolution.Owner));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = solution.SplitSolution(realTransferAmount);

            removedSolution.DoEntityReaction(targetSolution.Owner, ReactionMethod.Injection);

            targetSolution.Inject(removedSolution);

            Owner.PopupMessage(user,
                Loc.GetString("You transfer {0}u to {1:theName}", removedSolution.TotalVolume, targetSolution.Owner));
            Dirty();
            AfterInject();
        }

        private void AfterInject()
        {
            // Automatically set syringe to draw after completely draining it.
            if (Owner.GetComponent<SolutionContainerComponent>().CurrentVolume == 0)
            {
                ToggleState = InjectorToggleMode.Draw;
            }
        }

        private void TryDraw(ISolutionInteractionsComponent targetSolution, IEntity user)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent? solution) || solution.EmptyVolume == 0)
            {
                return;
            }

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(_transferAmount, targetSolution.DrawAvailable);

            if (realTransferAmount <= 0)
            {
                Owner.PopupMessage(user, Loc.GetString("{0:theName} is empty!", targetSolution.Owner));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = targetSolution.Draw(realTransferAmount);

            if (!solution.TryAddSolution(removedSolution))
            {
                return;
            }

            Owner.PopupMessage(user,
                Loc.GetString("Drew {0}u from {1:theName}", removedSolution.TotalVolume, targetSolution.Owner));
            Dirty();
            AfterDraw();
        }

        private void AfterDraw()
        {
            // Automatically set syringe to inject after completely filling it.
            if (Owner.GetComponent<SolutionContainerComponent>().EmptyVolume == 0)
            {
                ToggleState = InjectorToggleMode.Inject;
            }
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            Dirty();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            Owner.TryGetComponent(out SolutionContainerComponent? solution);

            var currentVolume = solution?.CurrentVolume ?? ReagentUnit.Zero;
            var maxVolume = solution?.MaxVolume ?? ReagentUnit.Zero;

            return new InjectorComponentState(currentVolume, maxVolume, ToggleState);
        }
    }
}
