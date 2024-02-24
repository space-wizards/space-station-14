using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.Lathe;

public sealed class LatheLubeSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheLubeComponent, LatheGetSpeedEvent>(OnGetSpeed);
    }

    private void OnGetSpeed(Entity<LatheLubeComponent> ent, ref LatheGetSpeedEvent args)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
            return;

        var used = solution.RemoveReagent(ent.Comp.Reagent, ent.Comp.Cost);
        var efficiency = (used / ent.Comp.Cost).Float();
        var reduction = ent.Comp.Reduction * efficiency;
        Log.Debug($"Reduction: {reduction} from {used}u of lube");
        args.Time *= 1f - reduction;
    }
}
