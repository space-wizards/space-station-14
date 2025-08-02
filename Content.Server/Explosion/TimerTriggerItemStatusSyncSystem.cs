using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;

namespace Content.Server.Explosion;

/// <summary>
/// Server-side system that updates <see cref="TimerTriggerItemStatusComponent"/> with current timer delay
/// so that it can be displayed on the client item status panel.
/// </summary>
public sealed class TimerTriggerItemStatusSyncSystem : EntitySystem
{
    private readonly Dictionary<EntityUid, float> _lastDelays = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<TimerTriggerItemStatusComponent, OnUseTimerTriggerComponent>();
        while (enumerator.MoveNext(out var uid, out var status, out var timer))
        {
            var timerDelay = TimeSpan.FromSeconds(timer.Delay);
            if (!_lastDelays.TryGetValue(uid, out var lastDelay) || lastDelay != timer.Delay)
            {
                status.Delay = timerDelay;
                Dirty(uid, status);
                _lastDelays[uid] = timer.Delay;
            }
        }
    }
}
