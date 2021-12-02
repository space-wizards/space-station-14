using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Inventory;

[Serializable, NetSerializable]
public class InventoryComponentState : ComponentState
{
    public InventoryComponentState(Dictionary<EquipmentSlotDefines.Slots, string> slotContainers)
    {
        SlotContainers = slotContainers;
    }

    public Dictionary<EquipmentSlotDefines.Slots, string> SlotContainers { get; }
}
