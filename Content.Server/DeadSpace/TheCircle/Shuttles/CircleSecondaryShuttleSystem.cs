// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.NoShuttleFTL;
using Content.Server.GameTicking;
using Content.Shared.DeadSpace.TheCircle.Shuttles;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.TheCircle.Shuttles;

public sealed class CircleSecondaryShuttleSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CircleSecondaryShuttleComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.TimerStarted)
            {
                if (_gameTicker.RunLevel != GameRunLevel.InRound)
                    continue;

                var remaining = component.UnlockDelay - _gameTicker.RoundDuration();
                component.UnlockAt = _timing.CurTime + TimeSpan.FromTicks(Math.Max(0, remaining.Ticks));
                component.TimerStarted = true;
                Dirty(uid, component);
            }

            if (component.Unlocked || _timing.CurTime < component.UnlockAt)
                continue;

            RemComp<NoShuttleFTLComponent>(uid);
            component.Unlocked = true;
            Dirty(uid, component);
        }
    }
}
