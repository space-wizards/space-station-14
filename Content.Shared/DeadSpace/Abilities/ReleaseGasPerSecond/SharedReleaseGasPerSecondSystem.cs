// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Timing;
using Content.Shared.DeadSpace.Abilities.ReleaseGasPerSecond.Components;

namespace Content.Shared.DeadSpace.Abilities.ReleaseGasPerSecond;

public abstract class SharedReleaseGasPerSecondSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var necromorfs = EntityQueryEnumerator<ReleaseGasPerSecondComponent>();
        while (necromorfs.MoveNext(out var ent, out var necro))
        {
            if (curTime > necro.NextEmitInfection)
            {
                var domainGasEvent = new DomainGasEvent();
                RaiseLocalEvent(ent, ref domainGasEvent);
            }
        }
    }
}
