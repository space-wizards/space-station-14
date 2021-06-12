using Content.Shared.Disposal.Components;
using Content.Shared.DragDrop;
using Robust.Shared.GameObjects;

namespace Content.Client.Disposal.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedDisposalUnitComponent))]
    public class DisposalUnitComponent : SharedDisposalUnitComponent
    {
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return false;
        }
    }
}
