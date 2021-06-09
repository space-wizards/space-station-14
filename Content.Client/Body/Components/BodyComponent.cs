using Content.Shared.Body.Components;
using Content.Shared.DragDrop;
using Robust.Shared.GameObjects;

namespace Content.Client.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IBody))]
    public class BodyComponent : SharedBodyComponent, IDraggable
    {
        bool IDraggable.CanStartDrag(StartDragDropEvent args)
        {
            return true;
        }

        bool IDraggable.CanDrop(CanDropEvent args)
        {
            return true;
        }
    }
}
