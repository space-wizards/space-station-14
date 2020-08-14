using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.GUI
{
    public class SharedStrippableComponent : Component
    {
        public override string Name => "Strippable";

        [NetSerializable, Serializable]
        public enum StrippingUiKey
        {
            Key,
        }
    }

    [NetSerializable, Serializable]
    public class StrippingInventoryButtonPressed : BoundUserInterfaceMessage
    {
        public EquipmentSlotDefines.Slots Slot { get; }

        public StrippingInventoryButtonPressed(EquipmentSlotDefines.Slots slot)
        {
            Slot = slot;
        }
    }

    [NetSerializable, Serializable]
    public class StrippingHandButtonPressed : BoundUserInterfaceMessage
    {
        public string Hand { get; }

        public StrippingHandButtonPressed(string hand)
        {
            Hand = hand;
        }
    }

    [NetSerializable, Serializable]
    public class StrippingBoundUserInterfaceState : BoundUserInterfaceState
    {
        public Dictionary<EquipmentSlotDefines.Slots, string> Inventory { get; }
        public Dictionary<string, string> Hands { get; }

        public StrippingBoundUserInterfaceState(Dictionary<EquipmentSlotDefines.Slots, string> inventory, Dictionary<string, string> hands)
        {
            Inventory = inventory;
            Hands = hands;
        }
    }
}
