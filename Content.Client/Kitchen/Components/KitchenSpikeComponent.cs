using Content.Shared.DragDrop;
using Content.Shared.Kitchen.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Kitchen.Components
{
    [RegisterComponent]
    internal sealed class KitchenSpikeComponent : SharedKitchenSpikeComponent
    {
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return true;
        }
    }
}
