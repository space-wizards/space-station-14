using Content.Client.Items.Components;
using Content.Shared.Item;
using Robust.Shared.GameStates;

namespace Content.Client.Clothing
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    [ComponentReference(typeof(ItemComponent))]
    [NetworkedComponent()]
    public sealed class ClothingComponent : ItemComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("femaleMask")]
        public FemaleClothingMask FemaleMask { get; } = FemaleClothingMask.UniformFull;

        public string? InSlot;
    }

    public enum FemaleClothingMask : byte
    {
        NoMask = 0,
        UniformFull,
        UniformTop
    }
}
