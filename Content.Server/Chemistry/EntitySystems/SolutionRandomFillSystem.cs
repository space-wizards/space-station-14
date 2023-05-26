using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionRandomFillSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomFillSolutionComponent, MapInitEvent>(OnRandomSolutionFillMapInit);
    }

    private void OnRandomSolutionFillMapInit(EntityUid uid, RandomFillSolutionComponent component, MapInitEvent args)
    {
        var target = _solutionsSystem.EnsureSolution(uid, component.Solution);
        var (reagent, quantity) = _proto.Index<WeightedRandomPrototype>(component.WeightedRandomId).PickWithQuantity(_random);

        if (!_proto.TryIndex<ReagentPrototype>(reagent, out _))
        {
            Logger.Error(
                $"Tried to add invalid reagent Id {reagent} using SolutionRandomQuantityFill.");
            return;
        }

        if (quantity <= -1)
        {
            quantity = component.Quantity;
        }

        target.AddReagent(reagent, quantity);
    }
}
