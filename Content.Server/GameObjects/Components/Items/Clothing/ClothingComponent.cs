using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects.Components.Items.Clothing
{
    [RegisterComponent]
    [ComponentReference(typeof(ItemComponent))]
    [ComponentReference(typeof(StorableComponent))]
    [ComponentReference(typeof(IItemComponent))]
    public class ClothingComponent : ItemComponent, IUse
    {
        public override string Name => "Clothing";
        public override uint? NetID => ContentNetIDs.CLOTHING;

        [ViewVariables]
        public SlotFlags SlotFlags = SlotFlags.PREVENTEQUIP; //Different from None, NONE allows equips if no slot flags are required

        private bool _quickEquipEnabled = true;
        private int _heatResistance;
        [ViewVariables(VVAccess.ReadWrite)]
        public int HeatResistance => _heatResistance;

        private string _clothingEquippedPrefix;
        [ViewVariables(VVAccess.ReadWrite)]
        public string ClothingEquippedPrefix
        {
            get
            {
                return _clothingEquippedPrefix;
            }
            set
            {
                Dirty();
                _clothingEquippedPrefix = value;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _clothingEquippedPrefix, "ClothingPrefix", null);

            // TODO: Writing.
            serializer.DataReadFunction("Slots", new List<string>(0), list =>
            {
                foreach (var slotflagsloaded in list)
                {
                    SlotFlags |= (SlotFlags)Enum.Parse(typeof(SlotFlags), slotflagsloaded.ToUpper());
                }
            });

            serializer.DataField(ref _quickEquipEnabled, "QuickEquip", true);
            serializer.DataFieldCached(ref _heatResistance, "HeatResistance", 323);
        }

        public override ComponentState GetComponentState()
        {
            return new ClothingComponentState(ClothingEquippedPrefix, EquippedPrefix);
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!_quickEquipEnabled) return false;
            if (!eventArgs.User.TryGetComponent(out InventoryComponent inv)
            ||  !eventArgs.User.TryGetComponent(out HandsComponent hands)) return false;

            foreach (var (slot, flag) in SlotMasks)
            {
                // We check if the clothing can be equipped in this slot.
                if ((SlotFlags & flag) == 0) continue;

                if (inv.TryGetSlotItem(slot, out ItemComponent item))
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
