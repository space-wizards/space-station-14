using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Shared.Body.Systems;

public sealed class StomachSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public const string DefaultSolutionName = "stomach";

    public bool CanTransferSolution(
        EntityUid uid,
        Solution solution,
        StomachComponent? stomach = null,
        SolutionContainerManagerComponent? solutions = null)
    {
        return Resolve(uid, ref stomach, ref solutions, logMissing: false)
            && _solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.Solution, out var stomachSolution)
            // TODO: For now no partial transfers. Potentially change by design
            && stomachSolution.CanAddSolution(solution);
    }

    public bool TryTransferSolution(
        EntityUid uid,
        Solution solution,
        StomachComponent? stomach = null,
        SolutionContainerManagerComponent? solutions = null)
    {
        if (!Resolve(uid, ref stomach, ref solutions, logMissing: false)
            || !_solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.Solution)
            || !CanTransferSolution(uid, solution, stomach, solutions))
        {
            return false;
        }

        _solutionContainerSystem.TryAddSolution(stomach.Solution.Value, solution);

        return true;
    }
}
