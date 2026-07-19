using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed partial class SolutionPurgeSystem : EntitySystem
{
    private static readonly EntityTimerId PurgeTimer = new("purge");

    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionPurgeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SolutionPurgeComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<SolutionPurgeComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnMapInit(Entity<SolutionPurgeComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextPurgeTime = _timing.CurTime + ent.Comp.Duration;
        Dirty(ent);
        Schedule(ent);
    }

    private void OnHandleState(Entity<SolutionPurgeComponent> ent, ref ComponentHandleState args)
    {
        Schedule(ent);
    }

    private void OnTimer(Entity<SolutionPurgeComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != PurgeTimer || !TryComp<SolutionComponent>(ent, out var solution))
            return;

        ent.Comp.NextPurgeTime = args.NextDeadline ?? ent.Comp.NextPurgeTime + ent.Comp.Duration;
        Dirty(ent);

        var preserved = ent.Comp.Preserve.ToArray();
        for (var i = 0u; i < args.ElapsedCount; i++)
        {
            _solutionContainer.SplitSolutionWithout((ent, solution),
                ent.Comp.Quantity,
                preserved);
        }
    }

    private void Schedule(Entity<SolutionPurgeComponent> ent)
    {
        _timers.SetTimerAt(ent, PurgeTimer, ent.Comp.NextPurgeTime, ent.Comp.Duration);
    }
}
