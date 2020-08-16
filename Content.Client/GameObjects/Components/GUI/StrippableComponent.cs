using Content.Client.GameObjects.Components.Items;
using Content.Client.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.GUI;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.GUI
{
    [RegisterComponent]
    public class StrippableComponent : SharedStrippableComponent, IClientDraggable
    {
        public bool ClientCanDropOn(CanDropEventArgs eventArgs)
        {
            return eventArgs.Target.HasComponent<HandsComponent>()
                   && eventArgs.Target != eventArgs.Dragged && eventArgs.Target == eventArgs.User;
        }

        public bool ClientCanDrag(CanDragEventArgs eventArgs)
        {
            return true;
        }
    }
}
