using Content.Server.Chemistry.EntitySystems;
using Content.Server.Spaceshroom.Components;
using Content.Shared.Chemistry;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Spaceshroom;

public sealed partial class FoodSpaceshroomSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodSpaceshroomComponent, MapInitEvent>(OnRandomSolutionFillMapInit);
    }

    public void OnRandomSolutionFillMapInit(EntityUid uid, FoodSpaceshroomComponent component, MapInitEvent args)
    {
        var target = _solutionsSystem.EnsureSolution(uid, component.Solution);

        string reagentId;
        int amount;

        var rand = _random.NextFloat();
        Logger.Debug(rand.ToString());

        if (rand >= 0.5f)
        {
            reagentId = "SpaceDrugs";
            amount = 10;
        }
        else if (rand >= 0.25f)
        {
            Logger.Debug("No effect");
            return;
        }
        else if (rand >= 0.1f)
        {
            reagentId = "Ephedrine";
            amount = 10;
        }
        else if (rand >= 0.05f)
        {
            reagentId = "Lexorin";
            amount = 10;
        }
        else
        {
            reagentId = "Amatoxin";
            amount = 15;
        }

        Logger.Debug(reagentId);
        target.AddReagent(reagentId, amount);
    }
}
