#nullable enable
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(IBody))]
    public class BodyComponent : SharedBodyComponent, IDraggable
    {
        bool IDraggable.CanStartDrag(StartDragDropEventArgs args)
        {
            return true;
        }

        public bool CanDrop(CanDropEventArgs args)
        {
            return true;
        }
    }
}
