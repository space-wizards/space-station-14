using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.IntegrationTests.Tests
{
    // Tests the behavior of HumanInventoryControllerComponent.
    // i.e. the interaction between uniforms and the pocket/ID slots.
    // and also how big items don't fit in pockets.
    [TestFixture]
    [TestOf(typeof(HumanInventoryControllerComponent))]
    public class HumanInventoryUniformSlotsTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();

            IEntity human = null;
            IEntity uniform = null;
            IEntity idCard = null;
            IEntity pocketItem = null;
            InventoryComponent inventory = null;

            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                var entityMan = IoCManager.Resolve<IEntityManager>();

                human = entityMan.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);
                uniform = entityMan.SpawnEntity("UniformJanitor", MapCoordinates.Nullspace);
                idCard = entityMan.SpawnEntity("AssistantIDCard", MapCoordinates.Nullspace);
                pocketItem = entityMan.SpawnEntity("FlashlightLantern", MapCoordinates.Nullspace);
                var tooBigItem = entityMan.SpawnEntity("ToolboxEmergency", MapCoordinates.Nullspace);

                inventory = human.GetComponent<InventoryComponent>();

                Assert.That(inventory.CanEquip(Slots.INNERCLOTHING, uniform));

                // Can't equip any of these since no uniform!
                Assert.That(inventory.CanEquip(Slots.IDCARD, idCard), Is.False);
                Assert.That(inventory.CanEquip(Slots.POCKET1, pocketItem), Is.False);
                Assert.That(inventory.CanEquip(Slots.POCKET1, tooBigItem), Is.False); // This one fails either way.

                inventory.Equip(Slots.INNERCLOTHING, uniform);

                Assert.That(inventory.Equip(Slots.IDCARD, idCard));
                Assert.That(inventory.Equip(Slots.POCKET1, pocketItem));
                Assert.That(inventory.CanEquip(Slots.POCKET1, tooBigItem), Is.False); // Still failing!

                Assert.That(IsDescendant(idCard, human));
                Assert.That(IsDescendant(pocketItem, human));

                // Now drop the jumpsuit.
                inventory.Unequip(Slots.INNERCLOTHING);
            });

            server.RunTicks(2);

            server.Assert(() =>
            {
                // Items have been dropped!
                Assert.That(IsDescendant(uniform, human), Is.False);
                Assert.That(IsDescendant(idCard, human), Is.False);
                Assert.That(IsDescendant(pocketItem, human), Is.False);

                // Ensure everything null here.
                Assert.That(inventory.GetSlotItem(Slots.INNERCLOTHING), Is.Null);
                Assert.That(inventory.GetSlotItem(Slots.IDCARD), Is.Null);
                Assert.That(inventory.GetSlotItem(Slots.POCKET1), Is.Null);
            });

            await server.WaitIdleAsync();
        }

        private static bool IsDescendant(IEntity descendant, IEntity parent)
        {
            var tmpParent = descendant.Transform.Parent;
            while (tmpParent != null)
            {
                if (tmpParent.Owner == parent)
                {
                    return true;
                }

                tmpParent = tmpParent.Parent;
            }

            return false;
        }
    }
}
