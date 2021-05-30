using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Disposal
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
