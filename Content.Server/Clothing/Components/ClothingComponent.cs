using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Robust.Shared.GameStates;

namespace Content.Server.Clothing.Components
{
    // Needed for client-side clothing component.
    [RegisterComponent]
    [ComponentReference(typeof(SharedClothingComponent))]
    public sealed class ClothingComponent : SharedClothingComponent
    {
    }
}
