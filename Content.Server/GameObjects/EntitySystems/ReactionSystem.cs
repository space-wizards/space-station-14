using System.Collections.Generic;
using System.Linq;
using Content.Server.Chemistry;
using Content.Server.GameObjects.Components.Chemistry;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// Checks <see cref="SolutionComponent"/>s for conditions for chemical reactions and performs them if they're met.
    /// </summary>
    class ReactionSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        private IReadOnlyList<ReactionPrototype> _reactions;
        private AudioSystem _audioSystem;

        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(SolutionComponent));

            _reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>().ToList();
            _audioSystem = EntitySystemManager.GetEntitySystem<AudioSystem>();
        }

        public override void Update(float frameTime)
        {
            //Iterate through entities with SolutionComponents
            foreach (var entity in RelevantEntities)
            {
                var solution = entity.GetComponent<SolutionComponent>();
                if (solution.CurrentVolume == 0)
                    continue; //Skip empty solutions

                //For each solution check every reaction
                foreach (var reaction in _reactions)
                {
                    if (SolutionValidReaction(solution, reaction, out int unitReactions))
                    {
                        PerformReaction(solution, reaction, unitReactions);
                        break; //Only perform one reaction per solution per update.
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a solution has the reactants required to cause a specified reaction.
        /// </summary>
        /// <param name="solution">The solution to check for reaction conditions.</param>
        /// <param name="reaction">The reaction whose reactants will be checked for in the solution.</param>
        /// <param name="unitReactions">The number of times the reaction can occur with the given solution.</param>
        /// <returns></returns>
        private bool SolutionValidReaction(SolutionComponent solution, ReactionPrototype reaction, out int unitReactions)
        {
            unitReactions = 10000000; //Set to some impossibly large number initially
            foreach (var reactant in reaction.Reactants)
            {
                if (!solution.ContainsReagent(reactant.Key, out int reagentQuantity))
                {
                    return false;
                }
                int currentUnitReactions = reagentQuantity / reactant.Value.Amount;
                if (currentUnitReactions < unitReactions)
                {
                    unitReactions = currentUnitReactions;
                }
            }
            return true;
        }

        /// <summary>
        /// Perform a reaction on a solution. This assumes all reaction criteria have already been checked and are met.
        /// </summary>
        /// <param name="solution">Solution to be reacted.</param>
        /// <param name="reaction">Reaction to occur.</param>
        /// <param name="unitReactions">The number of times to cause this reaction.</param>
        private void PerformReaction(SolutionComponent solution, ReactionPrototype reaction, int unitReactions)
        {
            //Remove non-catalysts
            foreach (var reactant in reaction.Reactants)
            {
                if (!reactant.Value.Catalyst)
                {
                    int amountToRemove = unitReactions * reactant.Value.Amount;
                    solution.TryRemoveReagent(reactant.Key, amountToRemove);
                }
            }
            //Add products
            foreach (var product in reaction.Products)
            {
                solution.TryAddReagent(product.Key, (int)(unitReactions * product.Value), out int acceptedQuantity);
            }
            //Trigger reaction effects
            foreach (var effect in reaction.Effects)
            {
                effect.React(solution.Owner, unitReactions);
            }

            //Update dispenser UI
            solution.Dispenser?.UpdateUserInterface();
            //Play reaction sound client-side
            _audioSystem.Play("/Audio/effects/chemistry/bubbles.ogg", solution.Owner.Transform.GridPosition);
        }
    }
}
