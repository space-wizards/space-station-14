using Content.Shared.Chemistry;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.Shared.GameObjects.EntitySystems
{
    public class ChemicalReactionSystem : EntitySystem
    {
        private IEnumerable<ReactionPrototype> _reactions;

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            _reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
        }

        /// <summary>
        /// Checks if a solution has the reactants required to cause a specified reaction.
        /// </summary>
        /// <param name="solution">The solution to check for reaction conditions.</param>
        /// <param name="reaction">The reaction whose reactants will be checked for in the solution.</param>
        /// <param name="unitReactions">The number of times the reaction can occur with the given solution.</param>
        /// <returns></returns>
        public bool SolutionValidReaction(Solution solution, ReactionPrototype reaction, out ReagentUnit unitReactions)
        {
            unitReactions = ReagentUnit.MaxValue; //Set to some impossibly large number initially
            foreach (var reactant in reaction.Reactants)
            {
                if (!solution.ContainsReagent(reactant.Key, out ReagentUnit reagentQuantity))
                {
                    return false;
                }
                var currentUnitReactions = reagentQuantity / reactant.Value.Amount;
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
        /// Remove the reactants from the solution, then returns a solution with all reagents created.
        /// </summary>
        /// <param name="solution">Solution to be reacted.</param>
        /// <param name="reaction">Reaction to occur.</param>
        /// <param name="unitReactions">The number of times to cause this reaction.</param>
        public Solution PerformReaction(Solution solution, IEntity owner, ReactionPrototype reaction, ReagentUnit unitReactions)
        {
            //Remove non-catalysts
            foreach (var reactant in reaction.Reactants)
            {
                if (!reactant.Value.Catalyst)
                {
                    var amountToRemove = unitReactions * reactant.Value.Amount;
                    solution.RemoveReagent(reactant.Key, amountToRemove);
                }
            }

            // Add products
            var products = new Solution();
            foreach (var product in reaction.Products)
            {
                products.AddReagent(product.Key, product.Value * unitReactions);
            }

            // Trigger reaction effects
            foreach (var effect in reaction.Effects)
            {
                effect.React(owner, unitReactions.Double());
            }

            return products;
        }

        public void CheckForReaction(Solution solution, IEntity owner)
        {
            bool checkForNewReaction = false;
            while (true)
            {
                //TODO: make a hashmap at startup and then look up reagents in the contents for a reaction
                //Check the solution for every reaction
                foreach (var reaction in _reactions)
                {
                    if (SolutionValidReaction(solution, reaction, out var unitReactions))
                    {
                        PerformReaction(solution, owner, reaction, unitReactions);
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
    }
}
