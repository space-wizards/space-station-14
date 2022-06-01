using Robust.Shared.Utility;

namespace Content.Client.SurveillanceCamera;

public sealed class SurveillanceCameraMonitorSystem : EntitySystem
{
    private readonly RemQueue<EntityUid> _toRemove = new();

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

                _toRemove.Add(comp.Owner);
            }
        }

        foreach (var uid in _toRemove)
        {
            EntityManager.RemoveComponent<ActiveSurveillanceCameraMonitorVisualsComponent>(uid);
        }

        _toRemove.List?.Clear();
    }

    public void AddTimer(EntityUid uid, Action onFinish)
    {
        var comp = EnsureComp<ActiveSurveillanceCameraMonitorVisualsComponent>(uid);
        comp.OnFinish = onFinish;
    }

    public void RemoveTimer(EntityUid uid)
    {
        EntityManager.RemoveComponent<ActiveSurveillanceCameraMonitorVisualsComponent>(uid);
    }
}
