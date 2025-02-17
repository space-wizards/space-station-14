using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Shared.Animals;
/// <summary>
///     Gives the ability to produce milkable reagents;
///     produces endlessly if the owner does not have a HungerComponent.
/// </summary>
public sealed class UdderSystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UdderComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, UdderComponent component, MapInitEvent args)
    {
        component.NextGrowth = _timing.CurTime + component.GrowthDelay;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<UdderComponent>();
        while (query.MoveNext(out var uid, out var udder))
        {
            if (_timing.CurTime < udder.NextGrowth)
                continue;

            udder.NextGrowth += udder.GrowthDelay;

            if (_mobState.IsDead(uid))
                continue;

            if (!_solutionContainerSystem.ResolveSolution(uid, udder.SolutionName, ref udder.Solution, out var solution))
                continue;

            if (solution.AvailableVolume == 0)
                continue;

            // Actually there is food digestion so no problem with instant reagent generation "OnFeed"
            if (EntityManager.TryGetComponent(uid, out HungerComponent? hunger))
            {
                // Is there enough nutrition to produce reagent?
                if (_hunger.GetHungerThreshold(hunger) < HungerThreshold.Okay)
                    continue;

                _hunger.ModifyHunger(uid, -udder.HungerUsage, hunger);
            }

            //TODO: toxins from bloodstream !?
            _solutionContainerSystem.TryAddReagent(udder.Solution.Value, udder.ReagentId, udder.QuantityPerUpdate, out _);
        }
    }
}
