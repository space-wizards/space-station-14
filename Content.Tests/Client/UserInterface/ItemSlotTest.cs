using Content.Client.UserInterface;
using NUnit.Framework;

namespace Content.Tests.Client.UserInterface
{
    [TestFixture]
    public class ItemSlotTest
    {
        [Test]
        public void TestCalculateCooldownLevel()
        {
            Assert.AreEqual(ItemSlotManager.CalculateCooldownLevel(0.5f), 4);
            Assert.AreEqual(ItemSlotManager.CalculateCooldownLevel(1), 8);
            Assert.AreEqual(ItemSlotManager.CalculateCooldownLevel(0), 0);
        }
    }
}
