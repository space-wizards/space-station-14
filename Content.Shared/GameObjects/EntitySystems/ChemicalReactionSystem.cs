using Content.Shared.Chemistry;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.Shared.GameObjects.EntitySystems
{
    public class ChemicalReactionSystem : EntitySystem
    {
        private IEnumerable<ReactionPrototype> _reactions;

        private const int MaxReactionIterations = 20;

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            _reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
        }

        /// <summary>
        ///     Performs all chemical reactions that can be run on a solution.
        ///     Removes the reactants from the solution, then returns a solution with all products.
        ///     WARNING: Does not trigger reactions between solution and new products.
        /// </summary>
        private Solution ProcessReactions(Solution solution, IEntity owner)
        {
            //TODO: make a hashmap at startup and then look up reagents in the contents for a reaction
            var overallProducts = new Solution();
            foreach (var reaction in _reactions)
            {
                if (solution.CanReact(reaction, out var unitReactions))
                {
                    var reactionProducts = solution.PerformReaction(reaction, unitReactions, owner);
                    overallProducts.AddSolution(reactionProducts);
                    break;
                }
            }
            return overallProducts;
        }

        /// <summary>
        ///     Continually react a solution until no more reactions occur.
        /// </summary>
        public void FullyReactSolution(Solution solution, IEntity owner)
        {
            for (var i = 0; i < MaxReactionIterations; i++)
            {
                var products = ProcessReactions(solution, owner);

                if (products.TotalVolume <= 0)
                    return;

                solution.AddSolution(products);
            }
            Logger.Error($"{nameof(Solution)} on {owner} (Uid: {owner.Uid}) could not finish reacting in under {MaxReactionIterations} loops.");
        }

        /// <summary>
        ///     Continually react a solution until no more reactions occur, with a volume constraint.
        ///     If a reaction's products would exceed the max volume, some product is deleted.
        /// </summary>
        public void FullyReactSolution(Solution solution, IEntity owner, ReagentUnit maxVolume)
        {
            for (var i = 0; i < MaxReactionIterations; i++)
            {
                var products = ProcessReactions(solution, owner);

                if (products.TotalVolume <= 0)
                    return;

                var totalVolume = solution.TotalVolume + products.TotalVolume;
                var excessVolume = totalVolume - maxVolume; 

                if (excessVolume > 0)
                {
                    products.RemoveSolution(excessVolume); //excess product is deleted to fit under volume limit
                }

                solution.AddSolution(products);
            }
            Logger.Error($"{nameof(Solution)} on {owner} (Uid: {owner.Uid}) could not finish reacting in under {MaxReactionIterations} loops.");
        }
    }
}
