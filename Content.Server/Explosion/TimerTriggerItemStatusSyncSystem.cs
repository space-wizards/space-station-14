using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using System;

namespace Content.Server.Explosion;

/// <summary>
/// Server-side system that updates <see cref="TimerTriggerItemStatusComponent"/> with current timer delay
/// so that it can be displayed on the client item status panel.
/// </summary>
public sealed class TimerTriggerItemStatusSyncSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<TimerTriggerItemStatusComponent, OnUseTimerTriggerComponent>();
        while (enumerator.MoveNext(out var uid, out var status, out var timer))
        {
            var timerDelay = TimeSpan.FromSeconds(timer.Delay);
            if (status.Delay != timerDelay)
            {
                status.Delay = timerDelay;
                Dirty(uid, status);
            }
        }
    }
}
