using Content.Shared.GameObjects.Components.Strap;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Strap
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrapComponent))]
    public class StrapComponent : SharedStrapComponent
    {
        public override bool DragDropOn(DragDropEventArgs eventArgs)
        {
            return false;
        }
    }
}
