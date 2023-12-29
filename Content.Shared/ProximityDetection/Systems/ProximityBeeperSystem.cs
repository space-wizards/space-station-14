using Content.Shared.ProximityDetection.Components;
using Content.Shared.ProximityDetector;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.ProximityDetection.Systems;


//This handles generic proximity beeper logic
public sealed class ProximityBeeperSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ProximityBeeperComponent, ProximityDetectionEvent>(OnFoundTarget);
        SubscribeLocalEvent<ProximityBeeperComponent, ProximityDetectionNoTargetEvent>(OnNoTarget);
    }
    private void OnNoTarget(EntityUid uid, ProximityBeeperComponent beeper, ref ProximityDetectionNoTargetEvent args)
    {
        ClearTarget(beeper);
        Dirty(uid,beeper);
    }

    private void OnFoundTarget(EntityUid uid, ProximityBeeperComponent beeper, ref ProximityDetectionEvent args)
    {
        UpdateTarget(beeper,args.FoundEntity, args.Distance);
        UpdateBeepInterval(beeper);
        Dirty(uid,beeper);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ProximityBeeperComponent>();
        while (query.MoveNext(out var uid, out var beeper))
        {
            if (!beeper.Enabled || beeper.TargetEnt == null)
                continue;

            if (_timing.CurTime < beeper.NextBeepTime)
                continue;
            UpdateBeep(uid, beeper);
        }
    }

    public void SetEnable(EntityUid owner, bool isEnabled, ProximityBeeperComponent? comp = null)
    {
        if (!Resolve(owner, ref comp))
            return;
        comp.Enabled = isEnabled;
        UpdateBeepInterval(comp);
    }

    public void SetMute(EntityUid owner, bool isMuted, ProximityBeeperComponent? comp = null)
    {
        if (!Resolve(owner, ref comp))
            return;
        comp.IsMuted = isMuted;
    }

    private void UpdateBeepInterval(ProximityBeeperComponent beeper)
    {
        if (beeper.TargetEnt == null)
            return;
        var scalingFactor = beeper.Distance / beeper.MaximumDistance;
        var interval = (beeper.MaxBeepInterval - beeper.MinBeepInterval) * scalingFactor + beeper.MinBeepInterval;
        beeper.NextBeepTime += interval;
    }

    private void ClearTarget(ProximityBeeperComponent beeper)
    {
        beeper.TargetEnt = null;
        beeper.NextBeepTime = _timing.CurTime;
    }

    private void UpdateTarget(ProximityBeeperComponent beeper, EntityUid targetEnt, float distance)
    {
        if (beeper.TargetEnt == null)
        {
            beeper.TargetEnt = targetEnt;
            beeper.Distance = distance;
            beeper.NextBeepTime = _timing.CurTime;
            return;
        }
        UpdateBeepInterval(beeper);
    }

    private void UpdateBeep(EntityUid uid, ProximityBeeperComponent beeper)
    {
        if (!beeper.Enabled || beeper.TargetEnt == null)
        {
            return;
        }
        if (!beeper.IsMuted)
            _audio.PlayPvs(beeper.BeepSound, uid);
        UpdateBeepInterval(beeper);
        Dirty(uid,beeper);
    }
}
