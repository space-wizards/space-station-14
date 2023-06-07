using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionRegenerationSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionRegenerationComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolutionRegenerationComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var regen, out var manager))
        {
            if (_timing.CurTime < regen.NextRegenTime)
                continue;

            // timer ignores if its full, it's just a fixed cycle
            regen.NextRegenTime = _timing.CurTime + regen.Duration;
            if (_solutionContainer.TryGetSolution(uid, regen.Solution, out var solution, manager))
                _solutionContainer.TryAddSolution(uid, solution, regen.Generated);
        }
    }

    private void OnUnpaused(EntityUid uid, SolutionRegenerationComponent comp, ref EntityUnpausedEvent args)
    {
        comp.NextRegenTime += args.PausedTime;
    }
}
