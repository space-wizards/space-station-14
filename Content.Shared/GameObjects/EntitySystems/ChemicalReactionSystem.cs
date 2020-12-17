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
        protected IEnumerable<ReactionPrototype> Reactions;

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            LoadReactions();
        }

        public void LoadReactions()
        {
            Reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
        }

        /// <summary>
        /// Checks if a solution has the reactants required to cause a specified reaction.
        /// </summary>
        public static bool SolutionValidReaction(Solution solution, ReactionPrototype reaction, out ReagentUnit unitReactions)
        {
            unitReactions = ReagentUnit.MaxValue; //Set to some impossibly large number initially
            foreach (var reactant in reaction.Reactants)
            {
                if (!solution.ContainsReagent(reactant.Key, out var reagentQuantity))
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
        /// Perform a reaction on a solution. This assumes all reaction criteria are met.
        /// Removes the reactants from the solution, then returns a solution with all reagents created.
        /// </summary>
        public static Solution PerformReaction(Solution solution, IEntity owner, ReactionPrototype reaction, ReagentUnit unitReactions)
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

        /// <summary>
        /// Performs all chemical reactions that can be run on a solution.
        /// WARNING: Does not trigger reactions between solution and new products.
        /// </summary>
        public Solution ProcessReactions(Solution solution, IEntity owner)
        {
            //TODO: make a hashmap at startup and then look up reagents in the contents for a reaction
            var overallProducts = new Solution();
            foreach (var reaction in Reactions)
            {
                if (SolutionValidReaction(solution, reaction, out var unitReactions))
                {
                    var reactionProducts = PerformReaction(solution, owner, reaction, unitReactions);
                    overallProducts.AddSolution(reactionProducts);
                    break;
                }
            }
            return overallProducts;
        }
    }
}
