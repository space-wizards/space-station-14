using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Disposal
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
