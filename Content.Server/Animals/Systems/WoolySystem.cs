using Content.Server.Animals.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Nutrition;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <summary>
///     Gives ability to produce fiber reagents, produces endless if the
///     owner has no SatiationComponent
/// </summary>
public sealed class WoolySystem : EntitySystem
{
    [Dependency] private readonly SatiationSystem _satiation = default!;
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
            if (EntityManager.TryGetComponent(uid, out SatiationComponent? satiation)
                && _satiation.TryGetSatiationThreshold((uid, satiation), wooly.UsedSatiation, out var threshold))
            {
                // Is there enough nutrition to produce reagent?
                if (threshold < SatiationThreashold.Okay)
                    continue;

                _satiation.ModifySatiation((uid, satiation), wooly.UsedSatiation, -wooly.SatiationUsage);
            }

            if (!_solutionContainer.ResolveSolution(uid, wooly.SolutionName, ref wooly.Solution))
                continue;

            _solutionContainer.TryAddReagent(wooly.Solution.Value, wooly.ReagentId, wooly.Quantity, out _);
        }
    }

    private void OnBeforeFullyEaten(Entity<WoolyComponent> ent, ref BeforeFullyEatenEvent args)
    {
        // don't want moths to delete goats after eating them
        args.Cancel();
    }
}
