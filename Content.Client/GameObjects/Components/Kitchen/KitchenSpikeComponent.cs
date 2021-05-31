#nullable enable
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Kitchen;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Kitchen
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
