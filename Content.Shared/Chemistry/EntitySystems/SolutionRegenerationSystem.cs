using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed partial class SolutionRegenerationSystem : EntitySystem
{
    private static readonly EntityTimerId RegenerationTimer = new("regeneration");

    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionRegenerationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SolutionRegenerationComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<SolutionRegenerationComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnMapInit(Entity<SolutionRegenerationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextRegenTime = _timing.CurTime + ent.Comp.Duration;

        Dirty(ent);
        Schedule(ent);
    }

    private void OnHandleState(Entity<SolutionRegenerationComponent> ent, ref ComponentHandleState args)
    {
        Schedule(ent);
    }

    private void OnTimer(Entity<SolutionRegenerationComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != RegenerationTimer || !TryComp<SolutionComponent>(ent, out var solution))
            return;

        ent.Comp.NextRegenTime = args.NextDeadline ?? ent.Comp.NextRegenTime + ent.Comp.Duration;
        Dirty(ent);
        for (var i = 0u; i < args.ElapsedCount; i++)
        {
            var amount = FixedPoint2.Min(solution.Solution.AvailableVolume, ent.Comp.Generated.Volume);
            if (amount <= FixedPoint2.Zero)
                break;

            var generated = amount == ent.Comp.Generated.Volume
                ? ent.Comp.Generated
                : ent.Comp.Generated.Clone().SplitSolution(amount);

            _solutionContainer.TryAddSolution((ent, solution), generated);
        }
    }

    private void Schedule(Entity<SolutionRegenerationComponent> ent)
    {
        _timers.SetTimerAt(ent, RegenerationTimer, ent.Comp.NextRegenTime, ent.Comp.Duration);
    }
}
