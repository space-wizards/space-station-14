using Content.Shared.Clothing.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Clothing
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedClothingComponent))]
    public sealed class ClothingComponent : SharedClothingComponent
    {
        public string? InSlot;
    }
}
