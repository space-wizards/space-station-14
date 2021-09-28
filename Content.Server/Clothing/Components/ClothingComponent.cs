using Content.Server.Hands.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Shared.Clothing;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Server.Clothing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    [ComponentReference(typeof(ItemComponent))]
    [NetworkedComponent()]
    public class ClothingComponent : ItemComponent, IUse
    {
        public override string Name => "Clothing";

        [ViewVariables]
        [DataField("Slots")]
        public SlotFlags SlotFlags = SlotFlags.PREVENTEQUIP; //Different from None, NONE allows equips if no slot flags are required

        [DataField("QuickEquip")]
        private bool _quickEquipEnabled = true;

        [DataField("HeatResistance")]
        private int _heatResistance = 323;

        [DataField("EquipSound")]
        public SoundSpecifier? EquipSound { get; set; } = default!;

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

        public bool TryEquip(InventoryComponent inv, Slots slot, IEntity user)
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
