using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Disposal;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposableComponent : SharedDisposableComponent, IDragDrop
    {
        bool IDragDrop.CanDragDrop(DragDropEventArgs eventArgs)
        {
            return eventArgs.Target.HasComponent<DisposalUnitComponent>();
        }

        bool IDragDrop.DragDrop(DragDropEventArgs eventArgs)
        {
            return eventArgs.Target.TryGetComponent(out DisposalUnitComponent unit) &&
                   unit.TryInsert(Owner);
        }
    }
}
