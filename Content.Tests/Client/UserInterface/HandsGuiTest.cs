using Content.Client.UserInterface;
using NUnit.Framework;

namespace Content.Tests.Client.UserInterface
{
    [TestFixture]
    public class HandsGuiTest
    {
        [Test]
        public void TestCalculateCooldownLevel()
        {
            Assert.AreEqual(HandsGui.CalculateCooldownLevel(0.5f), 4);
            Assert.AreEqual(HandsGui.CalculateCooldownLevel(1), 8);
            Assert.AreEqual(HandsGui.CalculateCooldownLevel(0), 0);
        }
    }
}
