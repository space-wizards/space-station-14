using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Robust.Shared.GameStates;

namespace Content.Server.Clothing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    [Virtual]
    public class ItemComponent : SharedItemComponent{}

    [RegisterComponent]
    [NetworkedComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    public sealed class ClothingComponent : ItemComponent
    {
    }

    // Needed for client-side clothing component.
    [RegisterComponent, NetworkedComponent]
    public sealed class NewClothingComponent : NewSharedClothingComponent
    {
    }
}
