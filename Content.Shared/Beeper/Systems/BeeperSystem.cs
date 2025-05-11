using Content.Shared.Beeper.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Item.ItemToggle;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Beeper.Systems;

/// <summary>
/// This handles generic proximity beeper logic.
/// </summary>
public sealed class BeeperSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeeperComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<BeeperComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextBeep = _timing.CurTime + ent.Comp.Interval;
        DirtyField(ent, ent.Comp, nameof(ent.Comp.NextBeep));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BeeperComponent>();

        while (query.MoveNext(out var uid, out var beeper))
        {
            if (beeper.NextBeep > _timing.CurTime)
                continue;

            beeper.NextBeep += beeper.Interval;
            DirtyField(uid, beeper, nameof(beeper.NextBeep));

            if (!_toggle.IsActivated(uid))
                continue;

            var beepEvent = new BeepPlayedEvent(beeper.IsMuted);
            RaiseLocalEvent(uid, ref beepEvent);

            if (!beeper.IsMuted)
                _audio.PlayPredicted(beeper.BeepSound, uid, null);
        }
    }

    /// <summary>
    /// Sets beeper interval scaling. The higher the value, the more frequent beeper will beep.
    /// </summary>
    public void SetIntervalScaling(EntityUid uid, FixedPoint2 newScaling, BeeperComponent? beeper = null)
    {
        if (!Resolve(uid, ref beeper))
            return;

        newScaling = FixedPoint2.Clamp(newScaling, 0, 1);

        if (beeper.IntervalScaling == newScaling)
            return;

        beeper.IntervalScaling = FixedPoint2.Clamp(newScaling, 0, 1);
        DirtyField(uid, beeper, nameof(beeper.IntervalScaling));

        var interval = (beeper.MaxBeepInterval + beeper.MinBeepInterval) * beeper.IntervalScaling.Float() + beeper.MinBeepInterval;

        beeper.Interval = interval < beeper.MinBeepInterval ? beeper.MinBeepInterval : interval;
        DirtyField(uid, beeper, nameof(beeper.Interval));
    }

    /// <summary>
    /// Sets whether or not beeper should be muted.
    /// </summary>
    public void SetMute(EntityUid uid, bool isMuted, BeeperComponent? beeper = null)
    {
        if (!Resolve(uid, ref beeper))
            return;

        beeper.IsMuted = isMuted;
        DirtyField(uid, beeper, nameof(beeper.IsMuted));
    }
}
