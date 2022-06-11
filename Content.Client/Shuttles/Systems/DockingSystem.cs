using Content.Shared.Shuttles.Events;

namespace Content.Client.Shuttles.Systems;

public sealed class DockingSystem : EntitySystem
{
    public void StartAutodock(EntityUid uid)
    {
        RaiseNetworkEvent(new AutodockRequestEvent {Entity = uid});
    }

    public void StopAutodock(EntityUid uid)
    {
        RaiseNetworkEvent(new StopAutodockRequestEvent() {Entity = uid});
    }

    public void Undock(EntityUid uid)
    {
        RaiseNetworkEvent(new UndockRequestEvent() {Entity = uid});
    }
}
