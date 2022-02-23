using Content.Shared.Buckle.Components;
using Content.Shared.DragDrop;
using Robust.Shared.GameObjects;

namespace Content.Client.Buckle.Strap
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrapComponent))]
    public sealed class StrapComponent : SharedStrapComponent
    {
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return false;
        }
    }
}
