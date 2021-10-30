using System;
using System.Linq;
using Content.Shared.Inventory;
using NUnit.Framework;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Tests.Shared
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [TestOf(typeof(EquipmentSlotDefines))]
    public class EquipmentSlotDefinesTest
    {
        /// <summary>
        ///     Test that all slots are contained in <see cref="AllSlots" />
        /// </summary>
        [Test]
        public void TestAllSlotsContainsAll()
        {
            foreach (var slotObj in Enum.GetValues(typeof(Slots)))
            {
                var slot = (Slots) slotObj;

                if (slot == Slots.NONE || slot == Slots.LAST)
                {
                    // Not real slots, skip these.
                    continue;
                }

                Assert.That(AllSlots.Contains(slot));
            }
        }

        /// <summary>
        ///     Test that every slot has an entry in <see cref="SlotNames" />.
        /// </summary>
        [Test]
        public void TestSlotNamesContainsAll()
        {
            foreach (var slot in AllSlots)
            {
                Assert.That(SlotNames, Contains.Key(slot));
            }
        }

        /// <summary>
        ///     Test that every slot has an entry in <see cref="SlotMasks" />.
        /// </summary>
        [Test]
        public void TestSlotMasksContainsAll()
        {
            foreach (var slot in AllSlots)
            {
                Assert.That(SlotMasks, Contains.Key(slot));
            }
        }
    }
}
