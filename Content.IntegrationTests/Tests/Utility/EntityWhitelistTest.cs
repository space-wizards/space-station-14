using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Whitelist;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Utility
{
    [TestFixture]
    [TestOf(typeof(EntityWhitelist))]
    public sealed class EntityWhitelistTest
    {
        private const string InvalidComponent = "Sprite";
        private const string ValidComponent = "Physics";

        private static readonly string Prototypes = $@"
- type: Tag
  id: ValidTag
- type: Tag
  id: InvalidTag

- type: entity
  id: WhitelistDummy
  components:
  - type: ItemSlots
    slots:
      slotName:
        whitelist:
          prototypes:
          - ValidPrototypeDummy
          components:
          - {ValidComponent}
          tags:
          - ValidTag

- type: entity
  id: InvalidComponentDummy
  components:
  - type: {InvalidComponent}
- type: entity
  id: InvalidTagDummy
  components:
  - type: Tag
    tags:
    - InvalidTag

- type: entity
  id: ValidComponentDummy
  components:
  - type: {ValidComponent}
- type: entity
  id: ValidTagDummy
  components:
  - type: Tag
    tags:
    - ValidTag";

        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);
            var mapCoordinates = testMap.MapCoords;

            var sEntities = server.ResolveDependency<IEntityManager>();

            await server.WaitAssertion(() =>
            {
                var validComponent = sEntities.SpawnEntity("ValidComponentDummy", mapCoordinates);
                var validTag = sEntities.SpawnEntity("ValidTagDummy", mapCoordinates);

                var invalidComponent = sEntities.SpawnEntity("InvalidComponentDummy", mapCoordinates);
                var invalidTag = sEntities.SpawnEntity("InvalidTagDummy", mapCoordinates);

                // Test instantiated on its own
                var whitelistInst = new EntityWhitelist
                {
                    Components = new[] { $"{ValidComponent}"},
                    Tags = new() {"ValidTag"}
                };
                whitelistInst.UpdateRegistrations();
                Assert.That(whitelistInst, Is.Not.Null);

                Assert.That(whitelistInst.Components, Is.Not.Null);
                Assert.That(whitelistInst.Tags, Is.Not.Null);

                Assert.That(whitelistInst.IsValid(validComponent), Is.True);
                Assert.That(whitelistInst.IsValid(validTag), Is.True);

                Assert.That(whitelistInst.IsValid(invalidComponent), Is.False);
                Assert.That(whitelistInst.IsValid(invalidTag), Is.False);

                // Test from serialized
                var dummy = sEntities.SpawnEntity("WhitelistDummy", mapCoordinates);
                var whitelistSer = sEntities.GetComponent<ItemSlotsComponent>(dummy).Slots.Values.First().Whitelist;
                Assert.That(whitelistSer, Is.Not.Null);

                Assert.That(whitelistSer.Components, Is.Not.Null);
                Assert.That(whitelistSer.Tags, Is.Not.Null);

                Assert.That(whitelistSer.IsValid(validComponent), Is.True);
                Assert.That(whitelistSer.IsValid(validTag), Is.True);

                Assert.That(whitelistSer.IsValid(invalidComponent), Is.False);
                Assert.That(whitelistSer.IsValid(invalidTag), Is.False);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
