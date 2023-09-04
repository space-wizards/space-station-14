using Content.Server.Body.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Utility;

namespace Content.Server.Body.Systems
{
    public sealed class StomachSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public const string DefaultSolutionName = "stomach";

        public override void Initialize()
        {
            SubscribeLocalEvent<StomachComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        }

        public override void Update(float frameTime)
        {
            foreach (var (stomach, organ, sol)in EntityManager.EntityQuery<StomachComponent, OrganComponent, SolutionContainerManagerComponent>())
            {
                stomach.AccumulatedFrameTime += frameTime;

                if (stomach.AccumulatedFrameTime < stomach.UpdateInterval)
                    continue;

                stomach.AccumulatedFrameTime -= stomach.UpdateInterval;

                // Get our solutions
                if (!_solutionContainerSystem.TryGetSolution(stomach.Owner, DefaultSolutionName,
                        out var stomachSolution, sol))
                    continue;

                if (organ.Body is not { } body || !_solutionContainerSystem.TryGetSolution(body, stomach.BodySolutionName, out var bodySolution))
                    continue;

                var transferSolution = new Solution();

                var queue = new RemQueue<StomachComponent.ReagentDelta>();
                foreach (var delta in stomach.ReagentDeltas)
                {
                    delta.Increment(stomach.UpdateInterval);
                    if (delta.Lifetime > stomach.DigestionDelay)
                    {
                        if (stomachSolution.TryGetReagent(delta.ReagentQuantity.Reagent, out var reagent))
                        {
                            if (reagent.Quantity > delta.ReagentQuantity.Quantity)
                                reagent = new(reagent.Reagent, delta.ReagentQuantity.Quantity);

                            _solutionContainerSystem.RemoveReagent((stomach).Owner, stomachSolution, reagent);
                            transferSolution.AddReagent(reagent);
                        }

                        queue.Add(delta);
                    }
                }

                foreach (var item in queue)
                {
                    stomach.ReagentDeltas.Remove(item);
                }

                // Transfer everything to the body solution!
                _solutionContainerSystem.TryAddSolution(body, bodySolution, transferSolution);
            }
        }

        private void OnApplyMetabolicMultiplier(EntityUid uid, StomachComponent component,
            ApplyMetabolicMultiplierEvent args)
        {
            if (args.Apply)
            {
                component.UpdateInterval *= args.Multiplier;
                return;
            }

            // This way we don't have to worry about it breaking if the stasis bed component is destroyed
            component.UpdateInterval /= args.Multiplier;
            // Reset the accumulator properly
            if (component.AccumulatedFrameTime >= component.UpdateInterval)
                component.AccumulatedFrameTime = component.UpdateInterval;
        }

        public bool CanTransferSolution(EntityUid uid, Solution solution,
            SolutionContainerManagerComponent? solutions = null)
        {
            if (!Resolve(uid, ref solutions, false))
                return false;

            if (!_solutionContainerSystem.TryGetSolution(uid, DefaultSolutionName, out var stomachSolution, solutions))
                return false;

            // TODO: For now no partial transfers. Potentially change by design
            if (!stomachSolution.CanAddSolution(solution))
                return false;

            return true;
        }

        public bool TryTransferSolution(EntityUid uid, Solution solution,
            StomachComponent? stomach = null,
            SolutionContainerManagerComponent? solutions = null)
        {
            if (!Resolve(uid, ref stomach, ref solutions, false))
                return false;

            if (!_solutionContainerSystem.TryGetSolution(uid, DefaultSolutionName, out var stomachSolution, solutions)
                || !CanTransferSolution(uid, solution, solutions))
                return false;

            _solutionContainerSystem.TryAddSolution(uid, stomachSolution, solution);
            // Add each reagent to ReagentDeltas. Used to track how long each reagent has been in the stomach
            foreach (var reagent in solution.Contents)
            {
                stomach.ReagentDeltas.Add(new StomachComponent.ReagentDelta(reagent));
            }

            return true;
        }
    }
}
