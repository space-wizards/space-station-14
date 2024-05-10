using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Utility
{
    [TestFixture]
    [TestOf(typeof(EntityWhitelist))]
    public sealed class EntityWhitelistTest
    {
        private const string InvalidComponent = "Sprite";
        private const string ValidComponent = "Physics";

        [TestPrototypes]
        private const string Prototypes = $@"
- type: Tag
  id: WhitelistTestValidTag
- type: Tag
  id: WhitelistTestInvalidTag

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
          - WhitelistTestValidTag

- type: entity
  id: InvalidComponentDummy
  components:
  - type: {InvalidComponent}
- type: entity
  id: WhitelistTestInvalidTagDummy
  components:
  - type: Tag
    tags:
    - WhitelistTestInvalidTag

- type: entity
  id: ValidComponentDummy
  components:
  - type: {ValidComponent}
- type: entity
  id: WhitelistTestValidTagDummy
  components:
  - type: Tag
    tags:
    - WhitelistTestValidTag";

        [Test]
        public async Task Test()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var testMap = await pair.CreateTestMap();
            var mapCoordinates = testMap.MapCoords;

            var sEntities = server.ResolveDependency<IEntityManager>();

            await server.WaitAssertion(() =>
            {
                var validComponent = sEntities.SpawnEntity("ValidComponentDummy", mapCoordinates);
                var WhitelistTestValidTag = sEntities.SpawnEntity("WhitelistTestValidTagDummy", mapCoordinates);

                var invalidComponent = sEntities.SpawnEntity("InvalidComponentDummy", mapCoordinates);
                var WhitelistTestInvalidTag = sEntities.SpawnEntity("WhitelistTestInvalidTagDummy", mapCoordinates);

                // Test instantiated on its own
                var whitelistInst = new EntityWhitelist
                {
                    Components = new[] { $"{ValidComponent}" },
                    Tags = new() { "WhitelistTestValidTag" }
                };
                whitelistInst.UpdateRegistrations();
                Assert.That(whitelistInst, Is.Not.Null);

                Assert.Multiple(() =>
                {
                    Assert.That(whitelistInst.Components, Is.Not.Null);
                    Assert.That(whitelistInst.Tags, Is.Not.Null);
                });

                Assert.Multiple(() =>
                {
                    Assert.That(whitelistInst.IsValid(validComponent), Is.True);
                    Assert.That(whitelistInst.IsValid(WhitelistTestValidTag), Is.True);

                    Assert.That(whitelistInst.IsValid(invalidComponent), Is.False);
                    Assert.That(whitelistInst.IsValid(WhitelistTestInvalidTag), Is.False);
                });

                // Test from serialized
                var dummy = sEntities.SpawnEntity("WhitelistDummy", mapCoordinates);
                var whitelistSer = sEntities.GetComponent<ItemSlotsComponent>(dummy).Slots.Values.First().Whitelist;
                Assert.That(whitelistSer, Is.Not.Null);

                Assert.Multiple(() =>
                {
                    Assert.That(whitelistSer.Components, Is.Not.Null);
                    Assert.That(whitelistSer.Tags, Is.Not.Null);
                });

                Assert.Multiple(() =>
                {
                    Assert.That(whitelistSer.IsValid(validComponent), Is.True);
                    Assert.That(whitelistSer.IsValid(WhitelistTestValidTag), Is.True);

                    Assert.That(whitelistSer.IsValid(invalidComponent), Is.False);
                    Assert.That(whitelistSer.IsValid(WhitelistTestInvalidTag), Is.False);
                });
            });
            await pair.CleanReturnAsync();
        }
    }
}
