using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Chemistry.EntitySystems
{
    public partial class SolutionContainerSystem
    {
        public void Refill(Solution.Solution solutionHolder, Solution.Solution solution)
        {
            if (!solutionHolder.Owner.HasComponent<RefillableSolutionComponent>())
                return;

            TryAddSolution(solutionHolder, solution);
        }

        public void Inject(Solution.Solution solutionHolder, Solution.Solution solution)
        {
            if (!solutionHolder.Owner.HasComponent<InjectableSolutionComponent>())
                return;

            TryAddSolution(solutionHolder, solution);
        }

        public Solution.Solution Draw(Solution.Solution solutionHolder, ReagentUnit amount)
        {
            if (!solutionHolder.Owner.HasComponent<DrawableSolutionComponent>())
            {
                var newSolution = new Solution.Solution();
                newSolution.Owner = solutionHolder.Owner;
            }

            return SplitSolution(solutionHolder, amount);
        }

        public Solution.Solution Drain(Solution.Solution solutionHolder, ReagentUnit amount)
        {
            if (!solutionHolder.Owner.HasComponent<DrainableSolutionComponent>())
            {
                var newSolution = new Solution.Solution();
                newSolution.Owner = solutionHolder.Owner;
            }

            return SplitSolution(solutionHolder, amount);
        }

        public bool TryGetInjectableSolution(IEntity owner,
            [NotNullWhen(true)] out Solution.Solution? solution)
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

        public Solution.Solution? GetInjectableSolution(IEntity ownerEntity)
        {
            TryGetInjectableSolution(ownerEntity, out var solution);
            return solution;
        }

        public bool TryGetRefillableSolution(IEntity owner,
            [NotNullWhen(true)] out Solution.Solution? solution)
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
            [NotNullWhen(true)] out Solution.Solution? solution)
        {
            if (owner.TryGetComponent(out DrainableSolutionComponent? drainable) &&
                owner.TryGetComponent(out SolutionContainerManagerComponent? manager) &&
                manager.Solutions.TryGetValue(drainable.Solution, out solution))
            {
                solution.Owner = owner;
                return true;
            }

            solution = null;
            return false;
        }

        public bool TryGetDrawableSolution(IEntity owner,
            [NotNullWhen(true)] out Solution.Solution? solution)
        {
            if (owner.TryGetComponent(out DrawableSolutionComponent? drawable) &&
                owner.TryGetComponent(out SolutionContainerManagerComponent? manager) &&
                manager.Solutions.TryGetValue(drawable.Solution, out solution))
            {
                solution.Owner = owner;
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

        public bool TryGetFitsInDispenser(IEntity owner,
            [NotNullWhen(true)] out Solution.Solution? solution)
        {
            if (owner.TryGetComponent(out FitsInDispenserComponent? dispenserFits) &&
                owner.TryGetComponent(out SolutionContainerManagerComponent? manager) &&
                manager.Solutions.TryGetValue(dispenserFits.Solution, out solution))
            {
                return true;
            }

            solution = null;
            return false;
        }

        public bool TryGetDefaultSolution(IEntity? target,
            [NotNullWhen(true)] out Solution.Solution? solution)
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

        public void AddDrainable(IEntity owner, string name)
        {
            if (owner.Deleted)
                return;

            if (!owner.TryGetComponent(out DrainableSolutionComponent? component))
            {
                component = owner.AddComponent<DrainableSolutionComponent>();
            }

            component.Solution = name;
        }

        public void RemoveDrainable(IEntity owner)
        {
            if (owner.Deleted || !owner.HasComponent<DrainableSolutionComponent>())
                return;

            owner.RemoveComponent<DrainableSolutionComponent>();
        }

        public void AddRefillable(IEntity owner, string name)
        {
            if (owner.Deleted)
                return;

            if (!owner.TryGetComponent(out RefillableSolutionComponent? component))
            {
                component = owner.AddComponent<RefillableSolutionComponent>();
            }

            component.Solution = name;
        }

        public void RemoveRefillable(IEntity owner)
        {
            if (owner.Deleted || !owner.HasComponent<RefillableSolutionComponent>())
                return;

            owner.RemoveComponent<RefillableSolutionComponent>();
        }
    }
}
