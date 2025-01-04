using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Animals;

/// <summary>
///     Gives ability to produce fiber reagents;
///     produces endlessly if the owner has no HungerComponent.
/// </summary>
public sealed class WoolySystem : EntitySystem
{
    [Dependency] private readonly SatiationSystem _satiation = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private static readonly ProtoId<SatiationTypePrototype> HungerSatiation = "Hunger";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoolyComponent, BeforeFullyEatenEvent>(OnBeforeFullyEaten);
        SubscribeLocalEvent<WoolyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, WoolyComponent component, MapInitEvent args)
    {
        component.NextGrowth = _timing.CurTime + component.GrowthDelay;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WoolyComponent>();
        while (query.MoveNext(out var uid, out var wooly))
        {
            if (_timing.CurTime < wooly.NextGrowth)
                continue;

            wooly.NextGrowth += wooly.GrowthDelay;

            if (_mobState.IsDead(uid))
                continue;

            if (!_solutionContainer.ResolveSolution(uid, wooly.SolutionName, ref wooly.Solution, out var solution))
                continue;

            if (solution.AvailableVolume == 0)
                continue;

            // Actually there is food digestion so no problem with instant reagent generation "OnFeed"
            if (TryComp<SatiationComponent>(uid, out var satiation))
            {
                // Is there enough nutrition to produce reagent?
                if (_satiation.GetThresholdWithDeltaOrNull((uid, satiation), HungerSatiation, -wooly.HungerUsage) < SatiationThreshold.Okay)
                {
                    continue;
                }

                _satiation.ModifyValue((uid, satiation), HungerSatiation, -wooly.HungerUsage);
            }

            _solutionContainer.TryAddReagent(wooly.Solution.Value, wooly.ReagentId, wooly.Quantity, out _);
        }
    }

    private void OnBeforeFullyEaten(Entity<WoolyComponent> ent, ref BeforeFullyEatenEvent args)
    {
        // don't want moths to delete goats after eating them
        args.Cancel();
    }
}
