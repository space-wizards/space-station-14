using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Inventory;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Items.Clothing
{
    public partial class ClothingComponentData
    {
        [DataClassTarget("slotFlags")] public EquipmentSlotDefines.SlotFlags? SlotFlags;

        public void ExposeData(ObjectSerializer serializer)
        {
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
