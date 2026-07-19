using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Animals;

/// <summary>
///     Gives ability to produce fiber reagents;
///     produces endlessly if the owner has no HungerComponent.
/// </summary>
public sealed partial class WoolySystem : EntitySystem
{
    private static readonly EntityTimerId GrowthTimer = new("growth");

    [Dependency] private HungerSystem _hunger = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoolyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WoolyComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<WoolyComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnMapInit(EntityUid uid, WoolyComponent component, MapInitEvent args)
    {
        component.NextGrowth = _timing.CurTime + component.GrowthDelay;
        _timers.SetTimerAt<WoolyComponent>((uid, component), GrowthTimer, component.NextGrowth);
    }

    private void OnEntRemoved(Entity<WoolyComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution
        if (entity.Comp.Solution == null || args.Entity != entity.Comp.Solution.Value.Owner)
            return;

        // Clear our cached reference to the solution entity
        entity.Comp.Solution = null;
    }

    private void OnTimer(Entity<WoolyComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != GrowthTimer)
            return;

        var wooly = ent.Comp;
        wooly.NextGrowth = args.ScheduledTime + wooly.GrowthDelay;
        _timers.SetTimerAt(ent, GrowthTimer, wooly.NextGrowth);

        if (_mobState.IsDead(ent) ||
            !_solutionContainer.ResolveSolution(ent.Owner, wooly.SolutionName, ref wooly.Solution, out var solution) ||
            solution.AvailableVolume == 0)
            return;

        if (TryComp(ent, out HungerComponent? hunger))
        {
            if (_hunger.GetHungerThreshold(hunger) < HungerThreshold.Okay)
                return;

            _hunger.ModifyHunger(ent, -wooly.HungerUsage, hunger);
        }

        _solutionContainer.TryAddReagent(wooly.Solution!.Value, wooly.ReagentId, wooly.Quantity, out _);
    }
}
