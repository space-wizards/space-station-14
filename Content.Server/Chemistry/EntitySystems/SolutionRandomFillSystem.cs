using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionRandomFillSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionsSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomFillSolutionComponent, MapInitEvent>(OnRandomSolutionFillMapInit);
    }

    private void OnRandomSolutionFillMapInit(Entity<RandomFillSolutionComponent> entity, ref MapInitEvent args)
    {
        if (entity.Comp.WeightedRandomId == null)
            return;

        var pick = _proto.Index<WeightedRandomFillSolutionPrototype>(entity.Comp.WeightedRandomId).Pick(_random);

        var reagent = pick.reagent;
        var quantity = pick.quantity;

        if (!_proto.HasIndex<ReagentPrototype>(reagent))
        {
            Log.Error($"Tried to add invalid reagent Id {reagent} using SolutionRandomFill.");
            return;
        }

        _solutionsSystem.EnsureSolutionEntity(entity.Owner, entity.Comp.Solution, out var target , pick.quantity);
        if(target.HasValue)
            _solutionsSystem.TryAddReagent(target.Value, reagent, quantity);
    }
}
