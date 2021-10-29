using System.Diagnostics.CodeAnalysis;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.EntitySystems
{
    public partial class SolutionContainerSystem
    {
        public void Refill(EntityUid targetUid, Solution targetSolution, Solution addedSolution)
        {
            if (!EntityManager.HasComponent<RefillableSolutionComponent>(targetUid))
                return;

            TryAddSolution(targetUid, targetSolution, addedSolution);
        }

        public void Inject(EntityUid targetUid, Solution targetSolution, Solution addedSolution)
        {
            if (!EntityManager.HasComponent<InjectableSolutionComponent>(targetUid))
                return;

            TryAddSolution(targetUid, targetSolution, addedSolution);
        }

        public Solution Draw(EntityUid targetUid, Solution solution, ReagentUnit amount)
        {
            if (!EntityManager.HasComponent<DrawableSolutionComponent>(targetUid))
            {
                return new Solution();
            }

            return SplitSolution(targetUid, solution, amount);
        }

        public Solution Drain(EntityUid targetUid, Solution targetSolution, ReagentUnit amount)
        {
            if (!EntityManager.HasComponent<DrainableSolutionComponent>(targetUid))
            {
                return new Solution();
            }

            return SplitSolution(targetUid, targetSolution, amount);
        }

        public bool TryGetInjectableSolution(EntityUid targetUid,
            [NotNullWhen(true)] out Solution? solution)
        {
            if (EntityManager.TryGetComponent(targetUid, out InjectableSolutionComponent? injectable) &&
                EntityManager.TryGetComponent(targetUid, out SolutionContainerManagerComponent? manager) &&
                manager.Solutions.TryGetValue(injectable.Solution, out solution))
            {
                return true;
            }

            solution = null;
            return false;
        }

        public bool TryGetRefillableSolution(EntityUid targetUid,
            [NotNullWhen(true)] out Solution? solution)
        {
            if (EntityManager.TryGetComponent(targetUid, out RefillableSolutionComponent? refillable) &&
                EntityManager.TryGetComponent(targetUid, out SolutionContainerManagerComponent? manager) &&
                manager.Solutions.TryGetValue(refillable.Solution, out var refillableSolution))
            {
                solution = refillableSolution;
                return true;
            }

            solution = null;
            return false;
        }

        public bool TryGetDrainableSolution(EntityUid uid,
            [NotNullWhen(true)] out Solution? solution,
            DrainableSolutionComponent? drainable = null,
            SolutionContainerManagerComponent? manager = null)
        {
            if (Resolve(uid, ref drainable, ref manager) &&
                manager.Solutions.TryGetValue(drainable.Solution, out solution))
                return true;

            solution = null;
            return false;
        }

        public bool TryGetDrawableSolution(EntityUid uid,
            [NotNullWhen(true)] out Solution? solution,
            DrawableSolutionComponent? drawable = null,
            SolutionContainerManagerComponent? manager = null)
        {
            if (!Resolve(uid, ref drawable, ref manager)
                || !manager.Solutions.TryGetValue(drawable.Solution, out solution))
            {
                solution = null;
                return false;
            }

            return true;
        }

        public ReagentUnit DrainAvailable(EntityUid uid)
        {
            return !TryGetDrainableSolution(uid, out var solution)
                ? ReagentUnit.Zero
                : solution.CurrentVolume;
        }

        public bool TryGetFitsInDispenser(EntityUid owner,
            [NotNullWhen(true)] out Solution? solution)
        {
            if (EntityManager.TryGetEntity(owner, out var ownerEntity) &&
                ownerEntity.TryGetComponent(out FitsInDispenserComponent? dispenserFits) &&
                ownerEntity.TryGetComponent(out SolutionContainerManagerComponent? manager) &&
                manager.Solutions.TryGetValue(dispenserFits.Solution, out solution))
            {
                return true;
            }

            solution = null;
            return false;
        }
    }
}
