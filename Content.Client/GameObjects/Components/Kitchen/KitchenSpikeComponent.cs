#nullable enable
using Content.Shared.GameObjects.Components.Kitchen;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Kitchen
{
    [RegisterComponent]
    internal sealed class KitchenSpikeComponent : SharedKitchenSpikeComponent
    {
        public override bool DragDropOn(DragDropEventArgs eventArgs)
        {
            return true;
        }
    }
}
