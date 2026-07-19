using Robust.Shared.Timing;

namespace Content.Client.SurveillanceCamera;

public sealed partial class SurveillanceCameraMonitorSystem : EntitySystem
{
    private static readonly EntityTimerId CameraSwitchTimer = new("camera-switch");
    private static readonly TimeSpan CameraSwitchDelay = TimeSpan.FromSeconds(10);

    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveSurveillanceCameraMonitorVisualsComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnTimer(
        Entity<ActiveSurveillanceCameraMonitorVisualsComponent> ent,
        ref EntityTimerEvent args)
    {
        if (args.Id != CameraSwitchTimer)
            return;

        ent.Comp.OnFinish?.Invoke();
        RemCompDeferred<ActiveSurveillanceCameraMonitorVisualsComponent>(ent);
    }

    public void AddTimer(EntityUid uid, Action onFinish)
    {
        var comp = EnsureComp<ActiveSurveillanceCameraMonitorVisualsComponent>(uid);
        comp.OnFinish = onFinish;
        comp.Deadline = _timers.SetTimer<ActiveSurveillanceCameraMonitorVisualsComponent>(
            (uid, comp),
            CameraSwitchTimer,
            CameraSwitchDelay);
    }

    public void RemoveTimer(EntityUid uid)
    {
        RemCompDeferred<ActiveSurveillanceCameraMonitorVisualsComponent>(uid);
    }
}
