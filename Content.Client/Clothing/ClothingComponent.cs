using Content.Shared.Clothing.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Clothing
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedClothingComponent))]
    public sealed class ClothingComponent : SharedClothingComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("femaleMask")]
        public FemaleClothingMask FemaleMask = FemaleClothingMask.UniformFull;

        public string? InSlot;
    }

    public enum FemaleClothingMask : byte
    {
        NoMask = 0,
        UniformFull,
        UniformTop
    }
}
