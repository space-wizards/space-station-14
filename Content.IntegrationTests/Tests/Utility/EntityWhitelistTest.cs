using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Whitelist;

namespace Content.IntegrationTests.Tests.Utility
{
    [TestOf(typeof(EntityWhitelist))]
    public sealed class EntityWhitelistTest
    {
        private const string InvalidComponent = "Sprite";
        private const string ValidComponent = "Physics";

        private const string WhitelistDummyId = "WhitelistDummy";
        private const string ValidComponentDummyId = "ValidComponentDummy";
        private const string WhitelistTestValidTagDummyId = "WhitelistTestValidTagDummy";
        private const string InvalidComponentDummyId = "InvalidComponentDummy";
        private const string WhitelistTestInvalidTagDummyId = "WhitelistTestInvalidTagDummy";

        [TestPrototypes]
        private const string Prototypes = $@"
- type: Tag
  id: WhitelistTestValidTag

- type: Tag
  id: WhitelistTestInvalidTag

- type: entity
  id: {WhitelistDummyId}
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
  id: {InvalidComponentDummyId}
  components:
  - type: {InvalidComponent}

- type: entity
  id: {WhitelistTestInvalidTagDummyId}
  components:
  - type: Tag
    tags:
    - WhitelistTestInvalidTag

- type: entity
  id: {ValidComponentDummyId}
  components:
  - type: {ValidComponent}

- type: entity
  id: {WhitelistTestValidTagDummyId}
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

            var sEntities = server.EntMan;
            var sys = server.System<EntityWhitelistSystem>();

            await server.WaitAssertion(() =>
            {
                var validComponent = sEntities.SpawnEntity(ValidComponentDummyId, mapCoordinates);
                var whitelistTestValidTag = sEntities.SpawnEntity(WhitelistTestValidTagDummyId, mapCoordinates);

                var invalidComponent = sEntities.SpawnEntity(InvalidComponentDummyId, mapCoordinates);
                var whitelistTestInvalidTag = sEntities.SpawnEntity(WhitelistTestInvalidTagDummyId, mapCoordinates);

                // Test instantiated on its own
                var whitelistInst = new EntityWhitelist
                {
                    Components = new[] { $"{ValidComponent}" },
                    Tags = new() { "WhitelistTestValidTag" }
                };

                Assert.Multiple(() =>
                {
                    Assert.That(sys.IsValid(whitelistInst, validComponent), Is.True);
                    Assert.That(sys.IsValid(whitelistInst, ValidComponentDummyId), Is.True);
                    Assert.That(sys.IsValid(whitelistInst, whitelistTestValidTag), Is.True);
                    Assert.That(sys.IsValid(whitelistInst, WhitelistTestValidTagDummyId), Is.True);

                    Assert.That(sys.IsValid(whitelistInst, invalidComponent), Is.False);
                    Assert.That(sys.IsValid(whitelistInst, InvalidComponentDummyId), Is.False);
                    Assert.That(sys.IsValid(whitelistInst, whitelistTestInvalidTag), Is.False);
                    Assert.That(sys.IsValid(whitelistInst, WhitelistTestInvalidTagDummyId), Is.False);
                });

                // Test from serialized
                var dummy = sEntities.SpawnEntity(WhitelistDummyId, mapCoordinates);
                var whitelistSer = sEntities.GetComponent<ItemSlotsComponent>(dummy).Slots.Values.First().Whitelist;
                Assert.That(whitelistSer, Is.Not.Null);

                Assert.Multiple(() =>
                {
                    Assert.That(whitelistSer.Components, Is.Not.Null);
                    Assert.That(whitelistSer.Tags, Is.Not.Null);
                });

                Assert.Multiple(() =>
                {
                    Assert.That(sys.IsValid(whitelistSer, validComponent), Is.True);
                    Assert.That(sys.IsValid(whitelistInst, ValidComponentDummyId), Is.True);
                    Assert.That(sys.IsValid(whitelistSer, whitelistTestValidTag), Is.True);
                    Assert.That(sys.IsValid(whitelistInst, WhitelistTestValidTagDummyId), Is.True);

                    Assert.That(sys.IsValid(whitelistSer, invalidComponent), Is.False);
                    Assert.That(sys.IsValid(whitelistInst, InvalidComponentDummyId), Is.False);
                    Assert.That(sys.IsValid(whitelistSer, whitelistTestInvalidTag), Is.False);
                    Assert.That(sys.IsValid(whitelistInst, WhitelistTestInvalidTagDummyId), Is.False);
                });
            });
            await pair.CleanReturnAsync();
        }
    }
}
