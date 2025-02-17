// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Abilities.Evolution.Components;
using Robust.Shared.Timing;

namespace Content.Shared.DeadSpace.Abilities.Evolution;

public sealed partial class SharedEvolutionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EvolutionComponent, ComponentInit>(OnMapInit);
        SubscribeLocalEvent<EvolutionComponent, EntityUnpausedEvent>(OnEvolutionUnpause);
    }

    private void OnMapInit(EntityUid uid, EvolutionComponent component, ComponentInit args)
    {
        component.TimeUntilEvolution = TimeSpan.FromSeconds(component.Duration) + _timing.CurTime;
    }

    private void OnEvolutionUnpause(EntityUid uid, EvolutionComponent component, ref EntityUnpausedEvent args)
    {
        component.TimeUntilEvolution += args.PausedTime;
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var EvolutionQuery = EntityQueryEnumerator<EvolutionComponent>();
        while (EvolutionQuery.MoveNext(out var uid, out var comp))
        {
            if (curTime > comp.TimeUntilEvolution)
            {
                var readyEvolutionEvent = new ReadyEvolutionEvent();
                RaiseLocalEvent(uid, ref readyEvolutionEvent);
            }
        }
    }
}
