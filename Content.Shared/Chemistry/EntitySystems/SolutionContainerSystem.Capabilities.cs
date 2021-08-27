using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;

namespace Content.Shared.Chemistry.EntitySystems
{
    public partial class SolutionContainerSystem
    {
        public void Refill(Solution solutionHolder, Solution solution)
        {
            if (!_entityManager.TryGetEntity(solution.OwnerUid, out var solutionEntity)
                || !solutionEntity.HasComponent<RefillableSolutionComponent>())
                return;

            TryAddSolution(solutionHolder, solution);
        }

        public void Inject(Solution solutionHolder, Solution solution)
        {
            if (!_entityManager.TryGetEntity(solution.OwnerUid, out var solutionEntity)
                || !solutionEntity.HasComponent<InjectableSolutionComponent>())
                return;

            TryAddSolution(solutionHolder, solution);
        }

        public Solution Draw(Solution solution, ReagentUnit amount)
        {
            if (!_entityManager.TryGetEntity(solution.OwnerUid, out var solutionEntity)
                || !solutionEntity.HasComponent<DrawableSolutionComponent>())
            {
                var newSolution = new Solution();
                newSolution.OwnerUid = solution.OwnerUid;
            }

            return SplitSolution(solution, amount);
        }

        public Solution Drain(Solution solution, ReagentUnit amount)
        {
            if (!_entityManager.TryGetEntity(solution.OwnerUid, out var solutionEntity)
                || !solutionEntity.HasComponent<DrainableSolutionComponent>())
            {
                var newSolution = new Solution();
                newSolution.OwnerUid = solution.OwnerUid;
            }

            return SplitSolution(solution, amount);
        }

        public bool TryGetInjectableSolution(IEntity owner,
            [NotNullWhen(true)] out Solution? solution)
        {
            if (owner.TryGetComponent(out InjectableSolutionComponent? injectable) &&
                owner.TryGetComponent(out SolutionContainerManagerComponent? manager) &&
                manager.Solutions.TryGetValue(injectable.Solution, out solution))
            {
                return true;
            }

            solution = null;
            return false;
        }

        public bool TryGetRefillableSolution(IEntity owner,
            [NotNullWhen(true)] out Solution? solution)
        {
            if (owner.TryGetComponent(out RefillableSolutionComponent? refillable) &&
                owner.TryGetComponent(out SolutionContainerManagerComponent? manager) &&
                manager.Solutions.TryGetValue(refillable.Solution, out solution))
            {
                return true;
            }

            solution = null;
            return false;
        }

        public bool TryGetDrainableSolution(IEntity owner,
            [NotNullWhen(true)] out Solution? solution)
        {
            if (owner.TryGetComponent(out DrainableSolutionComponent? drainable) &&
                owner.TryGetComponent(out SolutionContainerManagerComponent? manager) &&
                manager.Solutions.TryGetValue(drainable.Solution, out solution))
            {
                solution.OwnerUid = owner.Uid;
                return true;
            }

            solution = null;
            return false;
        }

        public bool TryGetDrawableSolution(IEntity owner,
            [NotNullWhen(true)] out Solution? solution)
        {
            if (owner.TryGetComponent(out DrawableSolutionComponent? drawable) &&
                owner.TryGetComponent(out SolutionContainerManagerComponent? manager) &&
                manager.Solutions.TryGetValue(drawable.Solution, out solution))
            {
                solution.OwnerUid = owner.Uid;
                return true;
            }

            solution = null;
            return false;
        }

        public ReagentUnit DrainAvailable(IEntity? owner)
        {
            if (owner == null || !TryGetDrainableSolution(owner, out var solution))
                return ReagentUnit.Zero;

            return solution.CurrentVolume;
        }

        public bool HasFitsInDispenser(IEntity owner)
        {
            return !owner.Deleted && owner.HasComponent<FitsInDispenserComponent>();
        }

        public bool TryGetFitsInDispenser(EntityUid owner,
            [NotNullWhen(true)] out Solution? solution)
        {
            if (_entityManager.TryGetEntity(owner, out var ownerEntity) &&
                ownerEntity.TryGetComponent(out FitsInDispenserComponent? dispenserFits) &&
                ownerEntity.TryGetComponent(out SolutionContainerManagerComponent? manager) &&
                manager.Solutions.TryGetValue(dispenserFits.Solution, out solution))
            {
                return true;
            }

            solution = null;
            return false;
        }

        public bool TryGetDefaultSolution(IEntity? target,
            [NotNullWhen(true)] out Solution? solution)
        {
            if (target == null
                || target.Deleted || !target.TryGetComponent(out SolutionContainerManagerComponent? solutionsMgr)
                || solutionsMgr.Solutions.Count != 1)
            {
                solution = null;
                return false;
            }

            solution = solutionsMgr.Solutions.Values.ToArray()[0];
            return true;
        }
    }
}
