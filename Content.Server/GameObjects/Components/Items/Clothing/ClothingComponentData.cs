using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Inventory;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Items.Clothing
{
    public partial class ClothingComponentData
    {
        [CustomYamlField("slotFlags")] public EquipmentSlotDefines.SlotFlags? SlotFlags;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: Writing.
            SlotFlags ??= EquipmentSlotDefines.SlotFlags.PREVENTEQUIP; //Different from None, NONE allows equips if no slot flags are required
            serializer.DataReadFunction("Slots", new List<string>(0), list =>
            {
                foreach (var slotflagsloaded in list)
                {
                    SlotFlags |= (EquipmentSlotDefines.SlotFlags)Enum.Parse(typeof(EquipmentSlotDefines.SlotFlags), slotflagsloaded.ToUpper());
                }
            });
            if (SlotFlags == EquipmentSlotDefines.SlotFlags.PREVENTEQUIP) SlotFlags = null;
        }
    }
}
