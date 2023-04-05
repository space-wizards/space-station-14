using System;
using System.Threading.Tasks;
using Content.Server.Stunnable;
using Content.Shared.Inventory;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class InventoryHelpersTest
    {
        private const string Prototypes = @"
- type: entity
  name: InventoryStunnableDummy
  id: InventoryStunnableDummy
  components:
  - type: Inventory
  - type: ContainerContainer
  - type: StatusEffects
    allowed:
    - Stun

- type: entity
  name: InventoryJumpsuitJanitorDummy
  id: InventoryJumpsuitJanitorDummy
  components:
  - type: Clothing
    slots: [innerclothing]

- type: entity
  name: InventoryIDCardDummy
  id: InventoryIDCardDummy
  components:
  - type: Clothing
    QuickEquip: false
    slots:
    - idcard
  - type: PDA
";
        [Test]
        public async Task SpawnItemInSlotTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();

            await server.WaitAssertion(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var systemMan = IoCManager.Resolve<IEntitySystemManager>();
                var human = sEntities.SpawnEntity("InventoryStunnableDummy", MapCoordinates.Nullspace);
                var invSystem = systemMan.GetEntitySystem<InventorySystem>();

                // Can't do the test if this human doesn't have the slots for it.
                Assert.That(invSystem.HasSlot(human, "jumpsuit"));
                Assert.That(invSystem.HasSlot(human, "id"));

                Assert.That(invSystem.SpawnItemInSlot(human, "jumpsuit", "InventoryJumpsuitJanitorDummy", true));

                // Do we actually have the uniform equipped?
                Assert.That(invSystem.TryGetSlotEntity(human, "jumpsuit", out var uniform));
                Assert.That(sEntities.GetComponent<MetaDataComponent>(uniform.Value).EntityPrototype is
                {
                    ID: "InventoryJumpsuitJanitorDummy"
                });

                systemMan.GetEntitySystem<StunSystem>().TryStun(human, TimeSpan.FromSeconds(1f), true);

                // Since the mob is stunned, they can't equip this.
                Assert.That(invSystem.SpawnItemInSlot(human, "id", "InventoryIDCardDummy", true), Is.False);

                // Make sure we don't have the ID card equipped.
                Assert.That(invSystem.TryGetSlotEntity(human, "item", out _), Is.False);

                // Let's try skipping the interaction check and see if it equips it!
                Assert.That(invSystem.SpawnItemInSlot(human, "id", "InventoryIDCardDummy", true, true));
                Assert.That(invSystem.TryGetSlotEntity(human, "id", out var idUid));
                Assert.That(sEntities.GetComponent<MetaDataComponent>(idUid.Value).EntityPrototype is
                {
                    ID: "InventoryIDCardDummy"
                });
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
