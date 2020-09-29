#nullable enable
using System;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Server behavior for reagent injectors and syringes. Can optionally support both
    /// injection and drawing or just injection. Can inject/draw reagents from solution
    /// containers, and can directly inject into a mobs bloodstream.
    /// </summary>
    [RegisterComponent]
    public class InjectorComponent : SharedInjectorComponent, IAfterInteract, IUse
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        /// <summary>
        /// Whether or not the injector is able to draw from containers or if it's a single use
        /// device that can only inject.
        /// </summary>
        [ViewVariables]
        private bool _injectOnly;

        /// <summary>
        /// Amount to inject or draw on each usage. If the injector is inject only, it will
        /// attempt to inject it's entire contents upon use.
        /// </summary>
        [ViewVariables]
        private ReagentUnit _transferAmount;

        /// <summary>
        /// Initial storage volume of the injector
        /// </summary>
        [ViewVariables]
        private ReagentUnit _initialMaxVolume;

        /// <summary>
        /// The state of the injector. Determines it's attack behavior. Containers must have the
        /// right SolutionCaps to support injection/drawing. For InjectOnly injectors this should
        /// only ever be set to Inject
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private InjectorToggleMode _toggleState;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _injectOnly, "injectOnly", false);
            serializer.DataField(ref _initialMaxVolume, "initialMaxVolume", ReagentUnit.New(15));
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(5));
        }
        protected override void Startup()
        {
            base.Startup();

            var solution = Owner.EnsureComponent<SolutionContainerComponent>();
            solution.Capabilities = SolutionContainerCaps.AddTo | SolutionContainerCaps.RemoveFrom;

            // Set _toggleState based on prototype
            _toggleState = _injectOnly ? InjectorToggleMode.Inject : InjectorToggleMode.Draw;
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
            switch (_toggleState)
            {
                case InjectorToggleMode.Inject:
                    _toggleState = InjectorToggleMode.Draw;
                    msg = "Now drawing";
                    break;
                case InjectorToggleMode.Draw:
                    _toggleState = InjectorToggleMode.Inject;
                    msg = "Now injecting";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Owner.PopupMessage(user, Loc.GetString(msg));

            Dirty();
        }

        /// <summary>
        /// Called when clicking on entities while holding in active hand
        /// </summary>
        /// <param name="eventArgs"></param>
        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true)) return;

            //Make sure we have the attacking entity
            if (eventArgs.Target == null || !Owner.TryGetComponent(out SolutionContainerComponent? solution))
            {
                return;
            }

            var targetEntity = eventArgs.Target;

            // Handle injecting/drawing for solutions
            if (targetEntity.TryGetComponent<SolutionContainerComponent>(out var targetSolution))
            {
                if (_toggleState == InjectorToggleMode.Inject)
                {
                    if (solution.CanRemoveSolutions && targetSolution.CanAddSolutions)
                    {
                        TryInject(targetSolution, eventArgs.User);
                    }
                    else
                    {
                        eventArgs.User.PopupMessage(eventArgs.User, Loc.GetString("You aren't able to transfer to {0:theName}!", targetSolution.Owner));
                    }
                }
                else if (_toggleState == InjectorToggleMode.Draw)
                {
                    if (targetSolution.CanRemoveSolutions && solution.CanAddSolutions)
                    {
                        TryDraw(targetSolution, eventArgs.User);
                    }
                    else
                    {
                        eventArgs.User.PopupMessage(eventArgs.User, Loc.GetString("You aren't able to draw from {0:theName}!", targetSolution.Owner));
                    }
                }
            }
            else // Handle injecting into bloodstream
            {
                if (targetEntity.TryGetComponent(out BloodstreamComponent? bloodstream) && _toggleState == InjectorToggleMode.Inject)
                {
                    if (solution.CanRemoveSolutions)
                    {
                        TryInjectIntoBloodstream(bloodstream, eventArgs.User);
                    }
                    else
                    {
                        eventArgs.User.PopupMessage(eventArgs.User, Loc.GetString("You aren't able to inject {0:theName}!", targetEntity));
                    }
                }
            }
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
                Owner.PopupMessage(user, Loc.GetString("You aren't able to inject {0:theName}!", targetBloodstream.Owner));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = solution.SplitSolution(realTransferAmount);

            if (!solution.CanAddSolution(removedSolution))
            {
                return;
            }

            // TODO: Account for partial transfer.

            foreach (var (reagentId, quantity) in removedSolution.Contents)
            {
                if(!_prototypeManager.TryIndex(reagentId, out ReagentPrototype reagent)) continue;
                removedSolution.RemoveReagent(reagentId, reagent.ReactionEntity(solution.Owner, ReactionMethod.Injection, quantity));
            }

            solution.TryAddSolution(removedSolution);

            foreach (var (reagentId, quantity) in removedSolution.Contents)
            {
                if(!_prototypeManager.TryIndex(reagentId, out ReagentPrototype reagent)) continue;
                reagent.ReactionEntity(targetBloodstream.Owner, ReactionMethod.Injection, quantity);
            }

            Owner.PopupMessage(user, Loc.GetString("You inject {0}u into {1:theName}!", removedSolution.TotalVolume, targetBloodstream.Owner));
            Dirty();
        }

        private void TryInject(SolutionContainerComponent targetSolution, IEntity user)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent? solution) || solution.CurrentVolume == 0)
            {
                return;
            }

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(_transferAmount, targetSolution.EmptyVolume);

            if (realTransferAmount <= 0)
            {
                Owner.PopupMessage(user, Loc.GetString("{0:theName} is already full!", targetSolution.Owner));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = solution.SplitSolution(realTransferAmount);

            if (!targetSolution.CanAddSolution(removedSolution))
            {
                return;
            }

            foreach (var (reagentId, quantity) in removedSolution.Contents)
            {
                if(!_prototypeManager.TryIndex(reagentId, out ReagentPrototype reagent)) continue;
                removedSolution.RemoveReagent(reagentId, reagent.ReactionEntity(targetSolution.Owner, ReactionMethod.Injection, quantity));
            }

            targetSolution.TryAddSolution(removedSolution);

            Owner.PopupMessage(user, Loc.GetString("You transfter {0}u to {1:theName}", removedSolution.TotalVolume, targetSolution.Owner));
            Dirty();
        }

        private void TryDraw(SolutionContainerComponent targetSolution, IEntity user)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent? solution) || solution.EmptyVolume == 0)
            {
                return;
            }

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(_transferAmount, targetSolution.CurrentVolume);

            if (realTransferAmount <= 0)
            {
                Owner.PopupMessage(user, Loc.GetString("{0:theName} is empty!", targetSolution.Owner));
                return;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = targetSolution.SplitSolution(realTransferAmount);

            if (!solution.TryAddSolution(removedSolution))
            {
                return;
            }

            Owner.PopupMessage(user, Loc.GetString("Drew {0}u from {1:theName}", removedSolution.TotalVolume, targetSolution.Owner));
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            Owner.TryGetComponent(out SolutionContainerComponent? solution);

            var currentVolume = solution?.CurrentVolume ?? ReagentUnit.Zero;
            var maxVolume = solution?.MaxVolume ?? ReagentUnit.Zero;

            return new InjectorComponentState(currentVolume, maxVolume, _toggleState);
        }
    }
}
