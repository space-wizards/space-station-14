using Content.Shared.Trigger.Components;

namespace Content.Server.Trigger.Systems;

/// <summary>
/// Server-side system that updates <see cref="TimerTriggerItemStatusComponent"/> with current timer delay.
/// </summary>
public sealed class TimerTriggerItemStatusSyncSystem : EntitySystem
{
    private readonly Dictionary<EntityUid, TimeSpan> _lastDelays = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<TimerTriggerItemStatusComponent, TimerTriggerComponent>();
        while (enumerator.MoveNext(out var uid, out var status, out var timer))
        {
            if (!_lastDelays.TryGetValue(uid, out var lastDelay) || lastDelay != timer.Delay)
            {
                status.Delay = timer.Delay;
                Dirty(uid, status);
                _lastDelays[uid] = timer.Delay;
            }
        }
    }
}
