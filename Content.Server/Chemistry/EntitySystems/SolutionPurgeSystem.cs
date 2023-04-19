using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
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
            purge.NextPurgeTime = _timing.CurTime + purge.Duration;
            if (_solutionContainer.TryGetSolution(uid, purge.Solution, out var solution, manager))
            {
                // purge.Quantity is the *total* quantity of reagent we want to remove.
                // We need to calculate how much *per reagent*, excluding reagents that
                // must be reserved.
                int reagentsToRemove = solution.Contents.Count;
                foreach (var reagentId in purge.Preserve)
                    if (solution.ContainsReagent(reagentId))
                        reagentsToRemove--;

                if (reagentsToRemove != 0)
                {
                    FixedPoint2 quantityPerReagent = purge.Quantity / reagentsToRemove;
                    foreach (var reagent in solution.Contents.ToArray())
                    {
                        if (purge.Preserve.Contains(reagent.ReagentId))
                            continue;
                        _solutionContainer.TryRemoveReagent(uid, solution, reagent.ReagentId, quantityPerReagent);
                    }
                }
            }
        }
    }

    private void OnUnpaused(EntityUid uid, SolutionPurgeComponent comp, ref EntityUnpausedEvent args)
    {
        comp.NextPurgeTime += args.PausedTime;
    }
}
