using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects
{
    public class ClothingComponent : ItemComponent, IUse
    {
        public override string Name => "Clothing";
        public override uint? NetID => ContentNetIDs.CLOTHING;
        public override Type StateType => typeof(ClothingComponentState);

        public SlotFlags SlotFlags = SlotFlags.PREVENTEQUIP; //Different from None, NONE allows equips if no slot flags are required

        private bool _quickEquipEnabled = true;
        private int _heatResistance;
        public int HeatResistance => _heatResistance;

        private string _clothingEquippedPrefix;
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

            foreach (var pair in SlotMasks)
            {
                if ((SlotFlags & pair.Value) == 0) continue;



                if (inv.TryGetSlotItem(pair.Key, out ItemComponent item))
                {
                    if (!inv.CanUnequip(pair.Key)) continue;
                    hands.Drop(Owner);
                    inv.Unequip(pair.Key);
                    hands.PutInHand(item);
                }
                else
                {
                    hands.Drop(Owner);
                }

                return inv.Equip(pair.Key, this);
            }

            return false;
        }
    }
}
