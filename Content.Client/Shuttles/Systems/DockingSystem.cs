using Content.Shared.Shuttles.Events;

namespace Content.Client.Shuttles.Systems;

public sealed class DockingSystem : EntitySystem
{
    public void StartAutodock(EntityUid uid)
    {
        RaiseNetworkEvent(new AutodockRequestMessage {Entity = uid});
    }

    public void StopAutodock(EntityUid uid)
    {
        RaiseNetworkEvent(new StopAutodockRequestMessage() {Entity = uid});
    }

    public void Undock(EntityUid uid)
    {
        RaiseNetworkEvent(new UndockRequestMessage() {Entity = uid});
    }
}
