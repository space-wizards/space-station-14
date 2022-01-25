using Content.Client.Items.Components;
using Content.Shared.Item;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Client.Clothing
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    [ComponentReference(typeof(ItemComponent))]
    [NetworkedComponent()]
    public class ClothingComponent : ItemComponent
    {
        public override string Name => "Clothing";

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
