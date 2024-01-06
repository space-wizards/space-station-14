using Content.Shared.Beeper.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Beeper.Systems;


//This handles generic proximity beeper logic
public sealed class BeeperSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BeeperComponent>();
        while (query.MoveNext(out var uid, out var beeper))
        {
            if (!beeper.Enabled)
                continue;
            RunUpdate_Internal(uid, beeper);
        }
    }

    public void SetEnable(EntityUid owner, bool isEnabled, BeeperComponent? beeper = null)
    {
        if (!Resolve(owner, ref beeper) || beeper.Enabled == isEnabled)
            return;
        beeper.Enabled = isEnabled;

        RunUpdate_Internal(owner, beeper);
        Dirty(owner, beeper);
    }

    public void SetIntervalScaling(EntityUid owner, BeeperComponent beeper, FixedPoint2 newScaling)
    {
        newScaling = FixedPoint2.Clamp(newScaling, 0, 1);
        beeper.IntervalScaling = newScaling;
        RunUpdate_Internal(owner, beeper);
        Dirty(owner, beeper);
    }

    public void SetInterval(EntityUid owner, BeeperComponent beeper, TimeSpan newInterval)
    {
        if (newInterval < beeper.MinBeepInterval)
            newInterval = beeper.MinBeepInterval;
        if (newInterval > beeper.MaxBeepInterval)
            newInterval = beeper.MaxBeepInterval;
        beeper.Interval = newInterval;
        RunUpdate_Internal(owner, beeper);
        Dirty(owner, beeper);
    }

    public void SetIntervalScaling(EntityUid owner, FixedPoint2 newScaling, BeeperComponent? beeper = null)
    {
        if (!Resolve(owner, ref beeper))
            return;
        SetIntervalScaling(owner, beeper, newScaling);
    }

    public void SetMute(EntityUid owner, bool isMuted, BeeperComponent? comp = null)
    {
        if (!Resolve(owner, ref comp))
            return;
        comp.IsMuted = isMuted;
    }

    private void UpdateBeepInterval(EntityUid owner, BeeperComponent beeper)
    {
        var scalingFactor = beeper.IntervalScaling.Float();
        var interval = (beeper.MaxBeepInterval - beeper.MinBeepInterval) * scalingFactor + beeper.MinBeepInterval;
        if (beeper.Interval == interval)
            return;
        beeper.Interval = interval;
        Dirty(owner, beeper);
    }

    public void ForceUpdate(EntityUid owner, BeeperComponent? beeper = null)
    {
        if (!Resolve(owner, ref beeper))
            return;
        RunUpdate_Internal(owner, beeper);
    }

    private void RunUpdate_Internal(EntityUid owner, BeeperComponent beeper)
    {
        if (!beeper.Enabled)
        {
            return;
        }
        UpdateBeepInterval(owner, beeper);
        if (beeper.NextBeep >= _timing.CurTime)
            return;
        var beepEvent = new BeepPlayedEvent(beeper.IsMuted);
        RaiseLocalEvent(owner, ref beepEvent);
        if (!beeper.IsMuted && _net.IsServer)
        {
            _audio.PlayPvs(beeper.BeepSound, owner);
        }
        beeper.LastBeepTime = _timing.CurTime;
    }
}
