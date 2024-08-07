using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Nutrition.EntitySystems;

public partial class FoodSequenceSystem : SharedFoodSequenceSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        //base.Initialize(); //Without dublicate subscription
    }

    public override void MergeFoodSolutions(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent> element)
    {
        if (!TryComp<FoodComponent>(start, out var startFood))
            return;

        if (!TryComp<FoodComponent>(element, out var elementFood))
            return;

        if (!_solutionContainer.TryGetSolution(start.Owner, startFood.Solution, out var startSolutionEntity, out var startSolution))
            return;

        if (!_solutionContainer.TryGetSolution(element.Owner, elementFood.Solution, out var elementSolutionEntity, out var elementSolution))
            return;

        startSolution.Volume += elementSolution.Volume;
        _solutionContainer.AddSolution(startSolutionEntity.Value, elementSolution);
    }
}
