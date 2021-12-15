using Content.Client.Inventory;
using Content.Client.Items.Components;
using Content.Shared.Clothing;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
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
        [DataField("femaleMask")] private FemaleClothingMask _femaleMask = FemaleClothingMask.UniformFull;
        public override string Name => "Clothing";

        private string? _clothingEquippedPrefix;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("ClothingPrefix")]
        public string? ClothingEquippedPrefix
        {
            get => _clothingEquippedPrefix;
            set
            {
                if (_clothingEquippedPrefix == value)
                    return;

                _clothingEquippedPrefix = value;

                if(!Initialized) return;

                if (!Owner.TryGetContainer(out IContainer? container))
                    return;
                if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(container.Owner, out ClientInventoryComponent? inventory))
                    return;
                if (!inventory.TryFindItemSlots(Owner, out EquipmentSlotDefines.Slots? slots))
                    return;

                inventory.SetSlotVisuals(slots.Value, Owner);
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            ClothingEquippedPrefix = ClothingEquippedPrefix;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public FemaleClothingMask FemaleMask
        {
            get => _femaleMask;
            set => _femaleMask = value;
        }
    }

    public enum FemaleClothingMask : byte
    {
        NoMask = 0,
        UniformFull,
        UniformTop
    }
}
