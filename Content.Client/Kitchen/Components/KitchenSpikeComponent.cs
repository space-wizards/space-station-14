using Content.Shared.Kitchen.Components;

namespace Content.Client.Kitchen.Components
{
    [RegisterComponent, ComponentReference(typeof(SharedKitchenSpikeComponent))]
    public sealed class KitchenSpikeComponent : SharedKitchenSpikeComponent
    {
    }
}
