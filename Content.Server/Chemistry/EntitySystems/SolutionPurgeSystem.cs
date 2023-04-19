using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
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

            // timer ignores if its full, it's just a fixed cycle
            purge.NextPurgeTime = _timing.CurTime + purge.Duration;
            if (_solutionContainer.TryGetSolution(uid, purge.Solution, out var solution, manager))
                _solutionContainer.TryAddSolution(uid, solution, purge.Purged);
        }
    }

    private void OnUnpaused(EntityUid uid, SolutionPurgeComponent comp, ref EntityUnpausedEvent args)
    {
        comp.NextPurgeTime += args.PausedTime;
    }
}
