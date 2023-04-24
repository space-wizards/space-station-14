using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionRandomFillSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomFillSolutionComponent, MapInitEvent>(OnRandomSolutionFillMapInit);
    }

    public void OnRandomSolutionFillMapInit(EntityUid uid, RandomFillSolutionComponent component, MapInitEvent args)
    {

        var target = _solutionsSystem.EnsureSolution(uid, component.Solution);
        var sumOfWeights = 0f;

        foreach (var picked  in component.RandomList)
        {
            sumOfWeights += picked.Weight;
        }

        sumOfWeights = _random.NextFloat(sumOfWeights);
        Solution? randSolution = null;

        foreach (var picked in component.RandomList)
        {
            sumOfWeights -= picked.Weight;

            if (sumOfWeights <= 0)
            {
                randSolution = picked.RandReagents;
                break;
            }
        }

        if (randSolution != null)
        {
            target.AddSolution(randSolution, null);
        }
    }
}
