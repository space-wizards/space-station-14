// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Abilities.HipoBite.Components;
using Robust.Shared.Timing;

namespace Content.Shared.DeadSpace.Abilities.HipoBite;

public sealed partial class SharedHipoBiteSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HipoBiteComponent, EntityUnpausedEvent>(OnRegenReagentUnpause);
    }

    private void OnRegenReagentUnpause(EntityUid uid, HipoBiteComponent component, ref EntityUnpausedEvent args)
    {
        component.TimeUntilRegenReagent += args.PausedTime;
        Dirty(uid, component);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var eggQuery = EntityQueryEnumerator<HipoBiteComponent>();
        while (eggQuery.MoveNext(out var uid, out var comp))
        {
            if (curTime > comp.TimeUntilRegenReagent)
            {
                var regenReagentForHipoBiteEvent = new RegenReagentForHipoBiteEvent();
                RaiseLocalEvent(uid, ref regenReagentForHipoBiteEvent);
            }

        }

    }


}
