using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Body.EntitySystems
{
    public class StomachSystem : EntitySystem
    {
        [Dependency] private BodySystem _bodySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        ///     Updates digestion status of ingested reagents.
        ///     Once reagents surpass _digestionDelay they are moved to the
        ///     bloodstream, where they are then metabolized.
        /// </summary>
        /// <param name="frameTime">
        ///     The time since the last update in seconds.
        /// </param>
        public override void Update(float frameTime)
        {
            foreach(var stom in ComponentManager.EntityQuery<StomachComponent>())
            {
                stom.AccumulatedFrameTime += frameTime;

                // Update at most once per second
                if (stom.AccumulatedFrameTime < stom.UpdateFrequency)
                {
                    return;
                }

                stom.AccumulatedFrameTime -= stom.UpdateFrequency;

                // Note that "Owner" should be the organ that has this behaviour/mechanism, and it should have a dedicated
                if (!stom.Owner.TryGetComponent(out SolutionContainerComponent? solution))
                {
                    return;
                }

                // Add reagents ready for transfer to bloodstream to transferSolution
                var transferSolution = new Solution();

                // Use ToList here to remove entries while iterating
                foreach (var delta in stom.ReagentDeltas.ToList())
                {
                    //Increment lifetime of reagents
                    delta.Increment(stom.UpdateFrequency);
                    if (delta.Lifetime > stom.DigestionDelay)
                    {
                        // This reagent has been in the stomach long enough, TRY to transfer it.
                        // But first, check if the reagent still exists, and how much is left.
                        // Some poor spessman may have washed down a potassium snack with some water.
                        if (solution.Solution.ContainsReagent(delta.ReagentId, out ReagentUnit quantity)){

                            if (quantity > delta.Quantity) {
                                quantity = delta.Quantity;
                            }

                            solution.TryRemoveReagent(delta.ReagentId, quantity);
                            transferSolution.AddReagent(delta.ReagentId, quantity);
                        }

                        stom.ReagentDeltas.Remove(delta);
                    }
                }

                // Transfer digested reagents to bloodstream, if we can
                if (stom.Mechanism != null
                    && stom.Mechanism.Body != null
                    && stom.Mechanism.Body.Owner.TryGetComponent<BloodstreamComponent>(out var blood))
                {
                    blood.TryTransferSolution(transferSolution);
                }
                else
                {
                    // Ah shit, nowhere to put it. Guess we'll keep it here for now.
                    solution.TryAddSolution(transferSolution);
                }
            }
        }

        public bool TryTransferSolution(StomachComponent stomach, Solution solution)
        {
            if (!stomach.Owner.TryGetComponent(out SolutionContainerComponent? solutionComponent))
            {
                return false;
            }

            // Add solution to _stomachContents
            solutionComponent.TryAddSolution(solution);
            // Add each reagent to _reagentDeltas. Used to track how long each reagent has been in the stomach
            foreach (var reagent in solution.Contents)
            {
                stomach.ReagentDeltas.Add(new StomachComponent.ReagentDelta(reagent.ReagentId, reagent.Quantity));
            }

            return true;
        }
    }
}
