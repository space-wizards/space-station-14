using Robust.Shared.Utility;

namespace Content.Client.SurveillanceCamera;

public sealed class SurveillanceCameraMonitorSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        foreach (var comp in EntityQuery<ActiveSurveillanceCameraMonitorVisualsComponent>())
        {
            if (Paused(comp.Owner))
            {
                continue;
            }

            comp.TimeLeft -= frameTime;

            if (comp.TimeLeft <= 0 || Deleted(comp.Owner))
            {
                if (comp.OnFinish != null)
                {
                    comp.OnFinish();
                }

                EntityManager.RemoveComponentDeferred<ActiveSurveillanceCameraMonitorVisualsComponent>(comp.Owner);
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
        EntityManager.RemoveComponentDeferred<ActiveSurveillanceCameraMonitorVisualsComponent>(uid);
    }
}
