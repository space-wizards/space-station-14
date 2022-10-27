using Content.Shared.Body.Components;
using Content.Shared.DragDrop;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Cryogenics;

public abstract class SharedCryoPodComponent: Component, IDragDropOn
{
    [Serializable, NetSerializable]
    public enum CryoPodVisuals : byte
    {
        IsOpen,
        PanelOpen,
        IsOn
    }

    public bool CanInsert(EntityUid entity)
    {
        return IoCManager.Resolve<IEntityManager>().HasComponent<BodyComponent>(entity);
    }

    bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
    {
        return CanInsert(eventArgs.Dragged);
    }

    public abstract bool DragDropOn(DragDropEvent eventArgs);
}
