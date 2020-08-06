using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.GUI
{
    [RegisterComponent]
    public class StrippableComponent : Component, IDragDrop
    {
        public override string Name => "Strippable";

        public bool CanDragDrop(DragDropEventArgs eventArgs)
        {
            throw new System.NotImplementedException();
        }

        public bool DragDrop(DragDropEventArgs eventArgs)
        {
            throw new System.NotImplementedException();
        }
    }
}
