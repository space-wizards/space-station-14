using System.Threading.Tasks;
using Content.Shared.Inventory;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    // Tests the behavior of InventoryComponent.
    // i.e. the interaction between uniforms and the pocket/ID slots.
    // and also how big items don't fit in pockets.
    [TestFixture]
    public sealed class HumanInventoryUniformSlotsTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Inventory
  - type: ContainerContainer

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
            await server.WaitIdleAsync();

            var invSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<InventorySystem>();
            var entityMan = server.ResolveDependency<IEntityManager>();
            var mapMan = server.ResolveDependency<IMapManager>();
            EntityUid human = default;
            EntityUid uniform = default;
            EntityUid idCard = default;
            EntityUid pocketItem = default;

            server.Assert(() =>
            {
                var mapId = mapMan.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                human = entityMan.SpawnEntity("HumanDummy", coordinates);
                uniform = entityMan.SpawnEntity("UniformDummy", coordinates);
                idCard = entityMan.SpawnEntity("IDCardDummy", coordinates);
                pocketItem = entityMan.SpawnEntity("FlashlightDummy", coordinates);
                var tooBigItem = entityMan.SpawnEntity("ToolboxDummy", coordinates);


                Assert.That(invSystem.CanEquip(human, uniform, "jumpsuit", out _));

                // Can't equip any of these since no uniform!
                Assert.That(invSystem.CanEquip(human, idCard, "id", out _), Is.False);
                Assert.That(invSystem.CanEquip(human, pocketItem, "pocket1", out _), Is.False);
                Assert.That(invSystem.CanEquip(human, tooBigItem, "pocket2", out _), Is.False); // This one fails either way.

                Assert.That(invSystem.TryEquip(human, uniform, "jumpsuit"));

                Assert.That(invSystem.TryEquip(human, idCard, "id"));
                Assert.That(invSystem.CanEquip(human, tooBigItem, "pocket1", out _), Is.False); // Still failing!
                Assert.That(invSystem.TryEquip(human, pocketItem, "pocket1"));

                Assert.That(IsDescendant(idCard, human, entityMan));
                Assert.That(IsDescendant(pocketItem, human, entityMan));

                // Now drop the jumpsuit.
                Assert.That(invSystem.TryUnequip(human, "jumpsuit"));
            });

            server.RunTicks(2);

            server.Assert(() =>
            {
                // Items have been dropped!
                Assert.That(IsDescendant(uniform, human, entityMan), Is.False);
                Assert.That(IsDescendant(idCard, human, entityMan), Is.False);
                Assert.That(IsDescendant(pocketItem, human, entityMan), Is.False);

                // Ensure everything null here.
                Assert.That(!invSystem.TryGetSlotEntity(human, "jumpsuit", out _));
                Assert.That(!invSystem.TryGetSlotEntity(human, "id", out _));
                Assert.That(!invSystem.TryGetSlotEntity(human, "pocket1", out _));
            });

            await server.WaitIdleAsync();
        }

        private static bool IsDescendant(EntityUid descendant, EntityUid parent, IEntityManager entManager)
        {
            var tmpParent = entManager.GetComponent<TransformComponent>(descendant).Parent;
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
