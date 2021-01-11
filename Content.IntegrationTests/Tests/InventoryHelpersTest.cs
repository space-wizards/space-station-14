using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Utility;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(InventoryHelpers))]
    public class InventoryHelpersTest : ContentIntegrationTest
    {
        private const string PROTOTYPES = @"
- type: entity
  name: InventoryStunnableDummy
  id: InventoryStunnableDummy
  components:
  - type: Inventory
  - type: Stunnable

- type: entity
  name: InventoryJumpsuitJanitorDummy
  id: InventoryJumpsuitJanitorDummy
  components:
  - type: Clothing
    Slots: [innerclothing]

- type: entity
  name: InventoryIDCardDummy
  id: InventoryIDCardDummy
  components:
  - type: Clothing
    QuickEquip: false
    Slots:
    - idcard
  - type: PDA
    idCard: AssistantIDCard
";
        [Test]
        public async Task SpawnItemInSlotTest()
        {
            var options = new ServerIntegrationOptions {ExtraPrototypes = PROTOTYPES};
            var server = StartServerDummyTicker(options);

            IEntity human = null;
            InventoryComponent inventory = null;
            StunnableComponent stun = null;

            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                var entityMan = IoCManager.Resolve<IEntityManager>();

                human = entityMan.SpawnEntity("InventoryStunnableDummy", MapCoordinates.Nullspace);
                inventory = human.GetComponent<InventoryComponent>();
                stun = human.GetComponent<StunnableComponent>();

                // Can't do the test if this human doesn't have the slots for it.
                Assert.That(inventory.HasSlot(Slots.INNERCLOTHING));
                Assert.That(inventory.HasSlot(Slots.IDCARD));

                Assert.That(inventory.SpawnItemInSlot(Slots.INNERCLOTHING, "InventoryJumpsuitJanitorDummy", true));

                // Do we actually have the uniform equipped?
                Assert.That(inventory.TryGetSlotItem(Slots.INNERCLOTHING, out ItemComponent uniform));
                Assert.That(uniform.Owner.Prototype != null && uniform.Owner.Prototype.ID == "InventoryJumpsuitJanitorDummy");

                stun.Stun(1f);

                // Since the mob is stunned, they can't equip this.
                Assert.That(inventory.SpawnItemInSlot(Slots.IDCARD, "InventoryIDCardDummy", true), Is.False);

                // Make sure we don't have the ID card equipped.
                Assert.That(inventory.TryGetSlotItem(Slots.IDCARD, out ItemComponent _), Is.False);

                // Let's try skipping the interaction check and see if it equips it!
                Assert.That(inventory.SpawnItemInSlot(Slots.IDCARD, "InventoryIDCardDummy", false));
                Assert.That(inventory.TryGetSlotItem(Slots.IDCARD, out ItemComponent id));
                Assert.That(id.Owner.Prototype != null && id.Owner.Prototype.ID == "InventoryIDCardDummy");
            });

            await server.WaitIdleAsync();
        }
    }
}
