using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Kitchen.Components;
using JetBrains.Annotations;

namespace Content.Shared.Kitchen.EntitySystems;

[UsedImplicitly]
public abstract class SharedReagentGrinderSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainersSystem = default!;

    public Solution? GetGrindSolution(EntityUid uid)
    {
        if (TryComp<ExtractableComponent>(uid, out var extractable)
            && extractable.GrindableSolution is not null
            && _solutionContainersSystem.TryGetSolution(uid, extractable.GrindableSolution, out _, out var solution))
        {
            return solution;
        }
        else
            return null;
    }

    public bool CanGrind(EntityUid uid)
    {
        var solutionName = CompOrNull<ExtractableComponent>(uid)?.GrindableSolution;

        return solutionName is not null && _solutionContainersSystem.TryGetSolution(uid, solutionName, out _, out _);
    }

    public bool CanJuice(EntityUid uid)
    {
        return CompOrNull<ExtractableComponent>(uid)?.JuiceSolution is not null;
    }
}

