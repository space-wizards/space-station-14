using Content.Server.Stunnable;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class InventoryHelpersTest
    {
        [TestPrototypes]
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
  - type: Pda
";
        [Test]
        public async Task SpawnItemInSlotTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();
            var systemMan = sEntities.EntitySysManager;

            await server.WaitAssertion(() =>
            {
                var human = sEntities.SpawnEntity("InventoryStunnableDummy", MapCoordinates.Nullspace);
                var invSystem = systemMan.GetEntitySystem<InventorySystem>();

                Assert.Multiple(() =>
                {
                    // Can't do the test if this human doesn't have the slots for it.
                    Assert.That(invSystem.HasSlot(human, "jumpsuit"));
                    Assert.That(invSystem.HasSlot(human, "id"));
                });

                Assert.That(invSystem.SpawnItemInSlot(human, "jumpsuit", "InventoryJumpsuitJanitorDummy", true));

#pragma warning disable NUnit2045
                // Do we actually have the uniform equipped?
                Assert.That(invSystem.TryGetSlotEntity(human, "jumpsuit", out var uniform));
                Assert.That(sEntities.GetComponent<MetaDataComponent>(uniform.Value).EntityPrototype is
                {
                    ID: "InventoryJumpsuitJanitorDummy"
                });
#pragma warning restore NUnit2045

                systemMan.GetEntitySystem<StunSystem>().TryStun(human, TimeSpan.FromSeconds(1f), true);

#pragma warning disable NUnit2045
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
#pragma warning restore NUnit2045
                sEntities.DeleteEntity(human);
            });

            await pair.CleanReturnAsync();
        }
    }
}
