using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionPurgeSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionPurgeComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolutionPurgeComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var purge, out var manager))
        {
            if (_timing.CurTime < purge.NextPurgeTime)
                continue;

            // timer ignores if it's empty, it's just a fixed cycle
            purge.NextPurgeTime += purge.Duration;
            if (_solutionContainer.TryGetSolution((uid, manager), purge.Solution, out var solution))
                _solutionContainer.SplitSolutionWithout(solution.Value, purge.Quantity, purge.Preserve.ToArray());
        }
    }

    private void OnUnpaused(Entity<SolutionPurgeComponent> entity, ref EntityUnpausedEvent args)
    {
        entity.Comp.NextPurgeTime += args.PausedTime;
    }
}
