using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects.Components.Items.Clothing
{
    [RegisterComponent]
    [ComponentReference(typeof(ItemComponent))]
    [ComponentReference(typeof(StorableComponent))]
    [ComponentReference(typeof(SharedStorableComponent))]
    [ComponentReference(typeof(IItemComponent))]
    public class ClothingComponent : ItemComponent, IUse
    {
        public override string Name => "Clothing";
        public override uint? NetID => ContentNetIDs.CLOTHING;

        [ViewVariables]
        [DataField("Slots")]
        public SlotFlags SlotFlags = SlotFlags.PREVENTEQUIP; //Different from None, NONE allows equips if no slot flags are required

        [DataField("QuickEquip")]
        private bool _quickEquipEnabled = true;

        [DataField("HeatResistance")]
        private int _heatResistance = 323;

        [ViewVariables(VVAccess.ReadWrite)]
        public int HeatResistance => _heatResistance;

        [DataField("ClothingPrefix")]
        private string? _clothingEquippedPrefix;
        [ViewVariables(VVAccess.ReadWrite)]
        public string? ClothingEquippedPrefix
        {
            get => _clothingEquippedPrefix;
            set
            {
                Dirty();
                _clothingEquippedPrefix = value;
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ClothingComponentState(ClothingEquippedPrefix, EquippedPrefix);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!_quickEquipEnabled) return false;
            if (!eventArgs.User.TryGetComponent(out InventoryComponent? inv)
            ||  !eventArgs.User.TryGetComponent(out HandsComponent? hands)) return false;

            foreach (var (slot, flag) in SlotMasks)
            {
                // We check if the clothing can be equipped in this slot.
                if ((SlotFlags & flag) == 0) continue;

                if (inv.TryGetSlotItem(slot, out ItemComponent? item))
                {
                    if (!inv.CanUnequip(slot)) continue;
                    hands.Drop(Owner);
                    inv.Unequip(slot);
                    hands.PutInHand(item);

                    if (!TryEquip(inv, slot, eventArgs.User))
                    {
                        hands.Drop(item.Owner);
                        inv.Equip(slot, item);
                        hands.PutInHand(Owner.GetComponent<ItemComponent>());
                    }
                }
                else
                {
                    hands.Drop(Owner);
                    if (!TryEquip(inv, slot, eventArgs.User))
                        hands.PutInHand(Owner.GetComponent<ItemComponent>());
                }

                return true;
            }

            return false;
        }

        private bool TryEquip(InventoryComponent inv, Slots slot, IEntity user)
        {
            if (!inv.Equip(slot, this, true, out var reason))
            {
                if (reason != null)
                    Owner.PopupMessage(user, reason);

                return false;
            }

            return true;
        }
    }
}
