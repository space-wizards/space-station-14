using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed class SolutionPurgeSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionPurgeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<SolutionPurgeComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextPurgeTime = _timing.CurTime + ent.Comp.Duration;
        Dirty(ent);
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
            // Needs to be networked and dirtied so that the client can reroll it during prediction
            Dirty(uid, purge);

            if (_solutionContainer.TryGetSolution((uid, manager), purge.Solution, out var solution))
            {
                _solutionContainer.SplitSolutionWithout(solution.Value,
                    purge.Quantity,
                    purge.Preserve.Select(proto => proto.Id).ToArray());
            }
        }
    }
}
