using System.Threading.Tasks;
using Content.Server.Inventory.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.IntegrationTests.Tests
{
    // Tests the behavior of HumanInventoryControllerComponent.
    // i.e. the interaction between uniforms and the pocket/ID slots.
    // and also how big items don't fit in pockets.
    [TestFixture]
    [TestOf(typeof(HumanInventoryControllerComponent))]
    public class HumanInventoryUniformSlotsTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Inventory
  - type: HumanInventoryController

- type: entity
  name: UniformDummy
  id: UniformDummy
  components:
  - type: Clothing
    Slots: [innerclothing]
    size: 5

- type: entity
  name: IDCardDummy
  id: IDCardDummy
  components:
  - type: Clothing
    Slots:
    - idcard
    size: 5
  - type: IdCard

- type: entity
  name: FlashlightDummy
  id: FlashlightDummy
  components:
  - type: Item
    size: 5

- type: entity
  name: ToolboxDummy
  id: ToolboxDummy
  components:
  - type: Item
    size: 9999
";
        [Test]
        public async Task Test()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServer(options);

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

                human = entityMan.SpawnEntity("HumanDummy", MapCoordinates.Nullspace);
                uniform = entityMan.SpawnEntity("UniformDummy", MapCoordinates.Nullspace);
                idCard = entityMan.SpawnEntity("IDCardDummy", MapCoordinates.Nullspace);
                pocketItem = entityMan.SpawnEntity("FlashlightDummy", MapCoordinates.Nullspace);
                var tooBigItem = entityMan.SpawnEntity("ToolboxDummy", MapCoordinates.Nullspace);

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
