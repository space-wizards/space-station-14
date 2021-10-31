using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Body.EntitySystems
{
    public class StomachSystem : EntitySystem
    {
        [Dependency] private BodySystem _bodySystem = default!;
        [Dependency] private SolutionContainerSystem _solutionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StomachComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, StomachComponent component, ComponentInit args)
        {
            component.StomachSolution = _solutionSystem.EnsureSolution(component.Owner, component.StomachSolutionName);
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
            foreach(var stom in EntityManager.EntityQuery<StomachComponent>())
            {
                stom.AccumulatedFrameTime += frameTime;

                // Update at most once per second
                if (stom.AccumulatedFrameTime < stom.UpdateFrequency)
                {
                    return;
                }

                stom.AccumulatedFrameTime -= stom.UpdateFrequency;

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
                        if (stom.StomachSolution.ContainsReagent(delta.ReagentId, out ReagentUnit quantity)){

                            if (quantity > delta.Quantity) {
                                quantity = delta.Quantity;
                            }

                            _solutionSystem.TryRemoveReagent(stom.Owner.Uid, stom.StomachSolution, delta.ReagentId, quantity);
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
                    _solutionSystem.TryAddSolution(stom.Owner.Uid, stom.StomachSolution, transferSolution);
                }
            }
        }
    }
}
