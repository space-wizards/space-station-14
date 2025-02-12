namespace Content.Client.SurveillanceCamera;

public sealed class SurveillanceCameraMonitorSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveSurveillanceCameraMonitorVisualsComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.TimeLeft -= frameTime;

            if (comp.TimeLeft <= 0)
            {
                comp.OnFinish?.Invoke();

                RemCompDeferred<ActiveSurveillanceCameraMonitorVisualsComponent>(uid);
            }
        }
    }

    public void AddTimer(EntityUid uid, Action onFinish)
    {
        var comp = EnsureComp<ActiveSurveillanceCameraMonitorVisualsComponent>(uid);
        comp.OnFinish = onFinish;
    }

    public void RemoveTimer(EntityUid uid)
    {
        RemCompDeferred<ActiveSurveillanceCameraMonitorVisualsComponent>(uid);
    }
}
