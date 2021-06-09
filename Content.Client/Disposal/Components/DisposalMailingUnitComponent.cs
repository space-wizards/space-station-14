using Content.Shared.Disposal.Components;
using Content.Shared.DragDrop;
using Robust.Shared.GameObjects;

namespace Content.Client.Disposal.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedDisposalMailingUnitComponent))]
    public class DisposalMailingUnitComponent : SharedDisposalMailingUnitComponent
    {
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return false;
        }
    }
}
