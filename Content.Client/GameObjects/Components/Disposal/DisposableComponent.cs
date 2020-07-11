using Content.Client.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Disposal;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposableComponent : SharedDisposableComponent, IClientDraggable
    {
        public bool ClientCanDropOn(CanDropEventArgs eventArgs)
        {
            return eventArgs.Target.HasComponent<DisposalUnitComponent>();
        }

        public bool ClientCanDrag(CanDragEventArgs eventArgs)
        {
            return true;
        }
    }
}
