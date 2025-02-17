// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Abilities.HipoHand.Components;
using Robust.Shared.Timing;

namespace Content.Shared.DeadSpace.Abilities.HipoHand;

public sealed partial class SharedHipoHandSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HipoHandComponent, EntityUnpausedEvent>(OnRegenReagentUnpause);
    }

    private void OnRegenReagentUnpause(EntityUid uid, HipoHandComponent component, ref EntityUnpausedEvent args)
    {
        component.TimeUntilRegenReagent += args.PausedTime;
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var eggQuery = EntityQueryEnumerator<HipoHandComponent>();
        while (eggQuery.MoveNext(out var uid, out var comp))
        {
            if (curTime > comp.TimeUntilRegenReagent)
            {
                var regenReagentEvent = new RegenReagentEvent();
                RaiseLocalEvent(uid, ref regenReagentEvent);
            }
        }
    }
}
