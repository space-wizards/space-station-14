#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Inventory
{
    public static class EquipmentSlotDefines
    {
        public static IReadOnlyCollection<Slots> AllSlots { get; }

        static EquipmentSlotDefines()
        {
            var output = new Slots[(int)Slots.LAST - (int)Slots.HEAD];

            // The index stuff is to jump over NONE.
            for (var i = 0; i < output.Length; i++)
            {
                output[i] = (Slots)(i+1);
            }

            AllSlots = output;
        }

        /// <summary>
        ///     Uniquely identifies a single slot in an inventory.
        /// </summary>
        [Serializable, NetSerializable]
        public enum Slots : byte
        {
            NONE = 0,
            HEAD,
            EYES,
            EARS,
            MASK,
            OUTERCLOTHING,
            INNERCLOTHING,
            NECK,
            BACKPACK,
            BELT,
            GLOVES,
            SHOES,
            IDCARD,
            POCKET1,
            POCKET2,

            /// <summary>
            ///     Not a real slot.
            /// </summary>
            LAST
        }

        /// <summary>
        ///     Defines what slot types an item can fit into.
        /// </summary>
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
            NECK = 1 << 7,
            BACK = 1 << 8,
            BACKPACK = 1 << 8,
            BELT = 1 << 9,
            GLOVES = 1 << 10,
            HAND = 1 << 10,
            IDCARD = 1 << 11,
            POCKET = 1 << 12,
            LEGS = 1 << 13,
            SHOES = 1 << 14,
            FEET = 1 << 14,
        }

        public static readonly IReadOnlyDictionary<Slots, string> SlotNames = new Dictionary<Slots, string>()
        {
            {Slots.HEAD, "Head"},
            {Slots.EYES, "Eyes"},
            {Slots.EARS, "Ears"},
            {Slots.MASK, "Mask"},
            {Slots.OUTERCLOTHING, "Outer Clothing"},
            {Slots.INNERCLOTHING, "Inner Clothing"},
            {Slots.NECK, "Neck"},
            {Slots.BACKPACK, "Backpack"},
            {Slots.BELT, "Belt"},
            {Slots.GLOVES, "Gloves"},
            {Slots.SHOES, "Shoes"},
            {Slots.IDCARD, "Id Card"},
            {Slots.POCKET1, "Left Pocket"},
            {Slots.POCKET2, "Right Pocket"},
        };

        /// <summary>
        ///     Defines which slot types fit in which slots.
        /// </summary>
        /// <remarks>
        ///     Note that this is not exhaustive. Inventory implementations can provide additional behavior.
        /// </remarks>
        public static readonly IReadOnlyDictionary<Slots, SlotFlags> SlotMasks = new Dictionary<Slots, SlotFlags>()
        {
            {Slots.HEAD, SlotFlags.HEAD},
            {Slots.EYES, SlotFlags.EYES},
            {Slots.EARS, SlotFlags.EARS},
            {Slots.MASK, SlotFlags.MASK},
            {Slots.OUTERCLOTHING, SlotFlags.OUTERCLOTHING},
            {Slots.INNERCLOTHING, SlotFlags.INNERCLOTHING},
            {Slots.NECK, SlotFlags.NECK},
            {Slots.BACKPACK, SlotFlags.BACK},
            {Slots.BELT, SlotFlags.BELT},
            {Slots.GLOVES, SlotFlags.GLOVES},
            {Slots.SHOES, SlotFlags.FEET},
            {Slots.IDCARD, SlotFlags.IDCARD},
            {Slots.POCKET1, SlotFlags.POCKET},
            {Slots.POCKET2, SlotFlags.POCKET},
        };

        // for shared string dict, since we don't define these anywhere in content
        [UsedImplicitly]
        public static readonly string[] _inventorySlotStrings =
        {
            "Inventory_HEAD",
            "Inventory_EYES",
            "Inventory_EARS",
            "Inventory_MASK",
            "Inventory_OUTERCLOTHING",
            "Inventory_INNERCLOTHING",
            "Inventory_NECK",
            "Inventory_BACKPACK",
            "Inventory_BELT",
            "Inventory_GLOVES",
            "Inventory_SHOES",
            "Inventory_IDCARD",
            "Inventory_POCKET1",
            "Inventory_POCKET2",
        };

        // for shared string dict, since we don't define these anywhere in content
        [UsedImplicitly]
        public static readonly string[] _handsSlotStrings =
        {
            "Hands_left",
            "Hands_right",
        };
    }
}
