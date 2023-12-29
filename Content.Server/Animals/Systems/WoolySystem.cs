using Content.Server.Animals.Components;
using Content.Server.Nutrition;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <summary>
///     Gives ability to produce fiber reagents, produces endless if the 
///     owner has no HungerComponent
/// </summary>
public sealed class WoolySystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoolyComponent, BeforeFullyEatenEvent>(OnBeforeFullyEaten);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WoolyComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var wooly))
        {
            if (now < wooly.NextGrowth)
                continue;

            wooly.NextGrowth = now + wooly.GrowthDelay;

            if (_mobState.IsDead(uid))
                continue;

            // Actually there is food digestion so no problem with instant reagent generation "OnFeed"
            if (EntityManager.TryGetComponent(uid, out HungerComponent? hunger))
            {
                // Is there enough nutrition to produce reagent?
                if (_hunger.GetHungerThreshold(hunger) < HungerThreshold.Okay)
                    continue;

                _hunger.ModifyHunger(uid, -wooly.HungerUsage, hunger);
            }

            if (!_solutionContainer.TryGetSolution(uid, wooly.Solution, out var solution))
                continue;

            _solutionContainer.TryAddReagent(uid, solution, wooly.ReagentId, wooly.Quantity, out _);
        }
    }

    private void OnBeforeFullyEaten(Entity<WoolyComponent> ent, ref BeforeFullyEatenEvent args)
    {
        // don't want moths to delete goats after eating them
        args.Cancel();
    }
}
