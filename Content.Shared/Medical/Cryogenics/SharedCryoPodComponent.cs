using Content.Shared.Body.Components;
using Content.Shared.DragDrop;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Cryogenics;

[NetworkedComponent]
public abstract class SharedCryoPodComponent: Component, IDragDropOn
{
    /// <summary>
    /// Container for mobs inserted in the pod.
    /// </summary>
    [ViewVariables, NonSerialized]
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// If true, the eject verb will not work on the pod and the user must use a crowbar to pry the pod open.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("locked")]
    public bool Locked { get; set; }

    /// <summary>
    /// Causes the pod to be locked without being fixable by messing with wires.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("permaLocked")]
    public bool PermaLocked { get; set; }

    public bool IsPrying { get; set; }

    [Serializable, NetSerializable]
    public enum CryoPodVisuals : byte
    {
        ContainsEntity,
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

    public virtual bool DragDropOn(DragDropEvent eventArgs)
    {
        return false;
    }
}
