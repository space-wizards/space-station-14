using System;
using System.Threading.Tasks;
using Content.Server.Inventory;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.Stunnable;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(InventoryHelpers))]
    public class InventoryHelpersTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: InventoryStunnableDummy
  id: InventoryStunnableDummy
  components:
  - type: Inventory
  - type: StatusEffects
    allowed:
    - Stun

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
";
        [Test]
        public async Task SpawnItemInSlotTest()
        {
            var options = new ServerIntegrationOptions {ExtraPrototypes = Prototypes};
            var server = StartServer(options);

            await server.WaitIdleAsync();

            var sEntities = server.ResolveDependency<IEntityManager>();

            EntityUid human = default;
            InventoryComponent inventory = null;

            await server.WaitAssertion(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                human = sEntities.SpawnEntity("InventoryStunnableDummy", MapCoordinates.Nullspace);
                inventory = sEntities.GetComponent<InventoryComponent>(human);

                // Can't do the test if this human doesn't have the slots for it.
                Assert.That(inventory.HasSlot(Slots.INNERCLOTHING));
                Assert.That(inventory.HasSlot(Slots.IDCARD));

                Assert.That(inventory.SpawnItemInSlot(Slots.INNERCLOTHING, "InventoryJumpsuitJanitorDummy", true));

                // Do we actually have the uniform equipped?
                Assert.That(inventory.TryGetSlotItem(Slots.INNERCLOTHING, out ItemComponent uniform));
                Assert.That(sEntities.GetComponent<MetaDataComponent>(uniform.Owner).EntityPrototype is
                {
                    ID: "InventoryJumpsuitJanitorDummy"
                });

                EntitySystem.Get<StunSystem>().TryStun(human, TimeSpan.FromSeconds(1f), true);

                // Since the mob is stunned, they can't equip this.
                Assert.That(inventory.SpawnItemInSlot(Slots.IDCARD, "InventoryIDCardDummy", true), Is.False);

                // Make sure we don't have the ID card equipped.
                Assert.That(inventory.TryGetSlotItem(Slots.IDCARD, out ItemComponent _), Is.False);

                // Let's try skipping the interaction check and see if it equips it!
                Assert.That(inventory.SpawnItemInSlot(Slots.IDCARD, "InventoryIDCardDummy"));
                Assert.That(inventory.TryGetSlotItem(Slots.IDCARD, out ItemComponent id));
                Assert.That(sEntities.GetComponent<MetaDataComponent>(id.Owner).EntityPrototype is
                {
                    ID: "InventoryIDCardDummy"
                });
            });
        }
    }
}
