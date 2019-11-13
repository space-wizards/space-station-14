using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Inventory
{
    public static class EquipmentSlotDefines
    {
        [Serializable, NetSerializable]
        public enum Slots
        {
            NONE,
            HEAD,
            EYES,
            EARS,
            MASK,
            OUTERCLOTHING,
            INNERCLOTHING,
            BACKPACK,
            BELT,
            GLOVES,
            SHOES,
            IDCARD,
            POCKET1,
            POCKET2,
            POCKET3,
            POCKET4,
            EXOSUITSLOT1,
            EXOSUITSLOT2
        }

        [Serializable, NetSerializable]
        [Flags]
        public enum SlotFlags
        {
            NONE = 0,
            PREVENTEQUIP = 1 << 0,
            HEAD = 1 << 1,
            HELMET = 1 << 1,
            EYES = 1 << 2,
            EARS = 1 << 3,
            MASK = 1 << 4,
            OUTERCLOTHING = 1 << 5,
            INNERCLOTHING = 1 << 6,
            BACK = 1 << 7,
            BACKPACK = 1 << 7,
            BELT = 1 << 8,
            GLOVES = 1 << 9,
            HAND = 1 << 9,
            IDCARD = 1 << 10,
            POCKET = 1 << 11,
            LEGS = 1 << 12,
            SHOES = 1 << 13,
            FEET = 1 << 13,
            EXOSUITSTORAGE = 1 << 14
        }

        public static Dictionary<Slots, string> SlotNames = new Dictionary<Slots, string>()
        {
            {Slots.HEAD, "Head" },
            {Slots.EYES, "Eyes" },
            {Slots.EARS, "Ears" },
            {Slots.MASK, "Mask" },
            {Slots.OUTERCLOTHING, "Outer Clothing" },
            {Slots.INNERCLOTHING, "Inner Clothing" },
            {Slots.BACKPACK, "Backpack" },
            {Slots.BELT, "Belt" },
            {Slots.GLOVES, "Gloves" },
            {Slots.SHOES, "Shoes" },
            {Slots.IDCARD, "Id Card" },
            {Slots.POCKET1, "Left Pocket" },
            {Slots.POCKET2, "Right Pocket" },
            {Slots.POCKET3, "Up Pocket" },
            {Slots.POCKET4, "Down Pocket" },
            {Slots.EXOSUITSLOT1, "Suit Storage" },
            {Slots.EXOSUITSLOT2, "Backup Storage" }
        };

        public static Dictionary<Slots, SlotFlags> SlotMasks = new Dictionary<Slots, SlotFlags>()
        {
            {Slots.HEAD, SlotFlags.HEAD },
            {Slots.EYES, SlotFlags.EYES },
            {Slots.EARS, SlotFlags.EARS },
            {Slots.MASK, SlotFlags.MASK },
            {Slots.OUTERCLOTHING, SlotFlags.OUTERCLOTHING },
            {Slots.INNERCLOTHING, SlotFlags.INNERCLOTHING },
            {Slots.BACKPACK, SlotFlags.BACK },
            {Slots.BELT, SlotFlags.BELT },
            {Slots.GLOVES, SlotFlags.GLOVES },
            {Slots.SHOES, SlotFlags.FEET },
            {Slots.IDCARD, SlotFlags.IDCARD },
            {Slots.POCKET1, SlotFlags.POCKET },
            {Slots.POCKET2, SlotFlags.POCKET },
            {Slots.POCKET3, SlotFlags.POCKET },
            {Slots.POCKET4, SlotFlags.POCKET },
            {Slots.EXOSUITSLOT1, SlotFlags.EXOSUITSTORAGE },
            {Slots.EXOSUITSLOT2, SlotFlags.EXOSUITSTORAGE }
        };
    }
}
