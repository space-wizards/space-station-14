using Content.Server.Animals.Components;
using Content.Server.Nutrition;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <summary>
/// Handles regeneration of an animal's wool solution when not hungry.
/// Shearing is not currently possible so the only use is for moths to eat.
/// </summary>
public sealed class WoolySystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoolyComponent, BeforeFullyEatenEvent>(OnBeforeFullyEaten);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WoolyComponent, HungerComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp, out var hunger))
        {
            if (now < comp.NextGrowth)
                continue;

            comp.NextGrowth = now + comp.GrowthDelay;

            // Is there enough nutrition to produce reagent?
            if (_hunger.GetHungerThreshold(hunger) < HungerThreshold.Peckish)
                continue;

            if (!_solutionContainer.TryGetSolution(uid, comp.Solution, out var solution))
                continue;

            _solutionContainer.TryAddReagent(uid, solution, comp.ReagentId, comp.Quantity, out _);
        }
    }

    private void OnBeforeFullyEaten(Entity<WoolyComponent> ent, ref BeforeFullyEatenEvent args)
    {
        // don't want moths to delete goats after eating them
        args.Cancel();
    }
}
