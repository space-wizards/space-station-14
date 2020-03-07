using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Content.Server.Chemistry;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    ///     Shared ECS component that manages a liquid solution of reagents.
    /// </summary>
    [RegisterComponent]
    internal class SolutionComponent : Shared.GameObjects.Components.Chemistry.SolutionComponent, IExamine
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly ILocalizationManager _loc;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        private IEnumerable<ReactionPrototype> _reactions;
        private AudioSystem _audioSystem;

        protected override void Startup()
        {
            base.Startup();
            Init();
        }

        public void Init()
        {
            _reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
            _audioSystem = _entitySystemManager.GetEntitySystem<AudioSystem>();
        }

        /// <summary>
        /// Initializes the SolutionComponent if it doesn't have an owner
        /// </summary>
        public void InitializeFromPrototype()
        {
            // Because Initialize needs an Owner, Startup isn't called, etc.
            IoCManager.InjectDependencies(this);
            _reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
        }

        /// <summary>
        ///     Transfers solution from the held container to the target container.
        /// </summary>
        [Verb]
        private sealed class FillTargetVerb : Verb<SolutionComponent>
        {
            protected override string GetText(IEntity user, SolutionComponent component)
            {
                if(!user.TryGetComponent<HandsComponent>(out var hands))
                    return "<I SHOULD BE INVISIBLE>";

                if(hands.GetActiveHand == null)
                    return "<I SHOULD BE INVISIBLE>";

                var heldEntityName = hands.GetActiveHand.Owner?.Prototype?.Name ?? "<Item>";
                var myName = component.Owner.Prototype?.Name ?? "<Item>";

                return $"Transfer liquid from [{heldEntityName}] to [{myName}].";
            }

            protected override VerbVisibility GetVisibility(IEntity user, SolutionComponent component)
            {
                if (user.TryGetComponent<HandsComponent>(out var hands))
                {
                    if (hands.GetActiveHand != null)
                    {
                        if (hands.GetActiveHand.Owner.TryGetComponent<SolutionComponent>(out var solution))
                        {
                            if ((solution.Capabilities & SolutionCaps.PourOut) != 0 && (component.Capabilities & SolutionCaps.PourIn) != 0)
                                return VerbVisibility.Visible;
                        }
                    }
                }

                return VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, SolutionComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands))
                    return;

                if (hands.GetActiveHand == null)
                    return;

                if (!hands.GetActiveHand.Owner.TryGetComponent<SolutionComponent>(out var handSolutionComp))
                    return;

                if ((handSolutionComp.Capabilities & SolutionCaps.PourOut) == 0 || (component.Capabilities & SolutionCaps.PourIn) == 0)
                    return;

                var transferQuantity = Math.Min(component.MaxVolume - component.CurrentVolume, handSolutionComp.CurrentVolume);
                transferQuantity = Math.Min(transferQuantity, 10);

                // nothing to transfer
                if (transferQuantity <= 0)
                    return;

                var transferSolution = handSolutionComp.SplitSolution(transferQuantity);
                component.TryAddSolution(transferSolution);

            }
        }

        void IExamine.Examine(FormattedMessage message)
        {
            message.AddText(_loc.GetString("Contains:\n"));
            foreach (var reagent in ReagentList)
            {
                if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                {
                    message.AddText($"{proto.Name}: {reagent.Quantity}u\n");
                }
                else
                {
                    message.AddText(_loc.GetString("Unknown reagent: {0}u\n", reagent.Quantity));
                }
            }
        }

        /// <summary>
        ///     Transfers solution from a target container to the held container.
        /// </summary>
        [Verb]
        private sealed class EmptyTargetVerb : Verb<SolutionComponent>
        {
            protected override string GetText(IEntity user, SolutionComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands))
                    return "<I SHOULD BE INVISIBLE>";

                if (hands.GetActiveHand == null)
                    return "<I SHOULD BE INVISIBLE>";

                var heldEntityName = hands.GetActiveHand.Owner?.Prototype?.Name ?? "<Item>";
                var myName = component.Owner.Prototype?.Name ?? "<Item>";

                return $"Transfer liquid from [{myName}] to [{heldEntityName}].";
            }

            protected override VerbVisibility GetVisibility(IEntity user, SolutionComponent component)
            {
                if (user.TryGetComponent<HandsComponent>(out var hands))
                {
                    if (hands.GetActiveHand != null)
                    {
                        if (hands.GetActiveHand.Owner.TryGetComponent<SolutionComponent>(out var solution))
                        {
                            if ((solution.Capabilities & SolutionCaps.PourIn) != 0 && (component.Capabilities & SolutionCaps.PourOut) != 0)
                                return VerbVisibility.Visible;
                        }
                    }
                }

                return VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, SolutionComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands))
                    return;

                if (hands.GetActiveHand == null)
                    return;

                if(!hands.GetActiveHand.Owner.TryGetComponent<SolutionComponent>(out var handSolutionComp))
                    return;

                if ((handSolutionComp.Capabilities & SolutionCaps.PourIn) == 0 || (component.Capabilities & SolutionCaps.PourOut) == 0)
                    return;

                var transferQuantity = Math.Min(handSolutionComp.MaxVolume - handSolutionComp.CurrentVolume, component.CurrentVolume);
                transferQuantity = Math.Min(transferQuantity, 10);

                // pulling from an empty container, pointless to continue
                if (transferQuantity <= 0)
                    return;

                var transferSolution = component.SplitSolution(transferQuantity);
                handSolutionComp.TryAddSolution(transferSolution);
            }
        }

        private void CheckForReaction()
        {
            bool checkForNewReaction = false;
            while (true)
            {
                //Check the solution for every reaction
                foreach (var reaction in _reactions)
                {
                    if (SolutionValidReaction(reaction, out int unitReactions))
                    {
                        PerformReaction(reaction, unitReactions);
                        checkForNewReaction = true;
                        break;
                    }
                }

                //Check for a new reaction if a reaction occurs, run loop again.
                if (checkForNewReaction)
                {
                    checkForNewReaction = false;
                    continue;
                }
                return;
            }
        }

        public bool TryAddReagent(string reagentId, int quantity, out int acceptedQuantity, bool skipReactionCheck = false, bool skipColor = false)
        {
            if (quantity > _maxVolume - _containedSolution.TotalVolume)
            {
                acceptedQuantity = _maxVolume - _containedSolution.TotalVolume;
                if (acceptedQuantity == 0) return false;
            }
            else
            {
                acceptedQuantity = quantity;
            }

            _containedSolution.AddReagent(reagentId, acceptedQuantity);
            if (!skipColor) {
                RecalculateColor();
            }
            if(!skipReactionCheck)
                CheckForReaction();
            OnSolutionChanged();
            return true;
        }

        public bool TryAddSolution(Solution solution, bool skipReactionCheck = false, bool skipColor = false)
        {
            if (solution.TotalVolume > (_maxVolume - _containedSolution.TotalVolume))
                return false;

            _containedSolution.AddSolution(solution);
            if (!skipColor) {
                RecalculateColor();
            }
            if(!skipReactionCheck)
                CheckForReaction();
            OnSolutionChanged();
            return true;
        }

        /// <summary>
        /// Checks if a solution has the reactants required to cause a specified reaction.
        /// </summary>
        /// <param name="solution">The solution to check for reaction conditions.</param>
        /// <param name="reaction">The reaction whose reactants will be checked for in the solution.</param>
        /// <param name="unitReactions">The number of times the reaction can occur with the given solution.</param>
        /// <returns></returns>
        private bool SolutionValidReaction(ReactionPrototype reaction, out int unitReactions)
        {
            unitReactions = int.MaxValue; //Set to some impossibly large number initially
            foreach (var reactant in reaction.Reactants)
            {
                if (!ContainsReagent(reactant.Key, out int reagentQuantity))
                {
                    return false;
                }
                int currentUnitReactions = reagentQuantity / reactant.Value.Amount;
                if (currentUnitReactions < unitReactions)
                {
                    unitReactions = currentUnitReactions;
                }
            }

            if (unitReactions == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Perform a reaction on a solution. This assumes all reaction criteria have already been checked and are met.
        /// </summary>
        /// <param name="solution">Solution to be reacted.</param>
        /// <param name="reaction">Reaction to occur.</param>
        /// <param name="unitReactions">The number of times to cause this reaction.</param>
        private void PerformReaction(ReactionPrototype reaction, int unitReactions)
        {
            //Remove non-catalysts
            foreach (var reactant in reaction.Reactants)
            {
                if (!reactant.Value.Catalyst)
                {
                    int amountToRemove = unitReactions * reactant.Value.Amount;
                    TryRemoveReagent(reactant.Key, amountToRemove);
                }
            }
            //Add products
            foreach (var product in reaction.Products)
            {
                TryAddReagent(product.Key, (int)(unitReactions * product.Value), out int acceptedQuantity, true);
            }
            //Trigger reaction effects
            foreach (var effect in reaction.Effects)
            {
                effect.React(Owner, unitReactions);
            }

            //Play reaction sound client-side
            _audioSystem.Play("/Audio/effects/chemistry/bubbles.ogg", Owner.Transform.GridPosition);
        }
    }
}
