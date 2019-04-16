using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects
{
    public class ClothingComponent : ItemComponent
    {
        public override string Name => "Clothing";
        public override uint? NetID => ContentNetIDs.CLOTHING;
        public override Type StateType => typeof(ClothingComponentState);

        public SlotFlags SlotFlags = SlotFlags.PREVENTEQUIP; //Different from None, NONE allows equips if no slot flags are required

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

            serializer.DataFieldCached(ref _heatResistance, "HeatResistance", 323);
        }

        public override ComponentState GetComponentState()
        {
            return new ClothingComponentState(ClothingEquippedPrefix, EquippedPrefix);
        }
    }
}
