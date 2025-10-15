using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Utility
{
    [TestOf(typeof(EntityWhitelist))]
    public sealed class EntityWhitelistTest
    {
        private const string InvalidComponent = "Sprite";
        private const string ValidComponent = "Physics";

        private const string WhitelistProtoId = "WhitelistDummy";
        private const string ValidComponentProtoId = "ValidComponentDummy";
        private const string WhitelistTestValidTagProtoId = "WhitelistTestValidTagDummy";
        private const string InvalidComponentProtoId = "InvalidComponentDummy";
        private const string WhitelistTestInvalidTagProtoId = "WhitelistTestInvalidTagDummy";

        [TestPrototypes]
        private const string Prototypes = $@"
- type: Tag
  id: WhitelistTestValidTag

- type: Tag
  id: WhitelistTestInvalidTag

- type: entity
  id: {WhitelistProtoId}
  components:
  - type: ItemSlots
    slots:
      slotName:
        whitelist:
          components:
          - {ValidComponent}
          tags:
          - WhitelistTestValidTag

- type: entity
  id: {InvalidComponentProtoId}
  components:
  - type: {InvalidComponent}

- type: entity
  id: {WhitelistTestInvalidTagProtoId}
  components:
  - type: Tag
    tags:
    - WhitelistTestInvalidTag

- type: entity
  id: {ValidComponentProtoId}
  components:
  - type: {ValidComponent}

- type: entity
  id: {WhitelistTestValidTagProtoId}
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
                var prototypeManager = server.ResolveDependency<IPrototypeManager>();

                var validComponentProto = prototypeManager.Index(ValidComponentProtoId);
                var whitelistTestValidTagProto = prototypeManager.Index(WhitelistTestValidTagProtoId);
                var invalidComponentProto = prototypeManager.Index(InvalidComponentProtoId);
                var whitelistTestInvalidTagProto = prototypeManager.Index(WhitelistTestInvalidTagProtoId);

                var validComponentUid = sEntities.SpawnEntity(ValidComponentProtoId, mapCoordinates);
                var whitelistTestValidTagUid = sEntities.SpawnEntity(WhitelistTestValidTagProtoId, mapCoordinates);
                var invalidComponentUid = sEntities.SpawnEntity(InvalidComponentProtoId, mapCoordinates);
                var whitelistTestInvalidTagUid = sEntities.SpawnEntity(WhitelistTestInvalidTagProtoId, mapCoordinates);

                // Test instantiated on its own
                var whitelistInst = new EntityWhitelist
                {
                    Components = new[] { $"{ValidComponent}" },
                    Tags = new() { "WhitelistTestValidTag" }
                };

                Assert.Multiple(() =>
                {
                    Assert.That(sys.IsValid(whitelistInst, validComponentUid), Is.True);
                    Assert.That(sys.IsValid(whitelistInst, validComponentProto), Is.True);
                    Assert.That(sys.IsValid(whitelistInst, ValidComponentProtoId), Is.True);

                    Assert.That(sys.IsValid(whitelistInst, whitelistTestValidTagUid), Is.True);
                    Assert.That(sys.IsValid(whitelistInst, whitelistTestValidTagProto), Is.True);
                    Assert.That(sys.IsValid(whitelistInst, WhitelistTestValidTagProtoId), Is.True);

                    Assert.That(sys.IsValid(whitelistInst, invalidComponentUid), Is.False);
                    Assert.That(sys.IsValid(whitelistInst, invalidComponentProto), Is.False);
                    Assert.That(sys.IsValid(whitelistInst, InvalidComponentProtoId), Is.False);

                    Assert.That(sys.IsValid(whitelistInst, whitelistTestInvalidTagUid), Is.False);
                    Assert.That(sys.IsValid(whitelistInst, whitelistTestInvalidTagProto), Is.False);
                    Assert.That(sys.IsValid(whitelistInst, WhitelistTestInvalidTagProtoId), Is.False);
                });

                // Test from serialized
                var dummy = sEntities.SpawnEntity(WhitelistProtoId, mapCoordinates);
                var whitelistSer = sEntities.GetComponent<ItemSlotsComponent>(dummy).Slots.Values.First().Whitelist;
                Assert.That(whitelistSer, Is.Not.Null);

                Assert.Multiple(() =>
                {
                    Assert.That(whitelistSer.Components, Is.Not.Null);
                    Assert.That(whitelistSer.Tags, Is.Not.Null);
                });

                Assert.Multiple(() =>
                {
                    Assert.That(sys.IsValid(whitelistSer, validComponentUid), Is.True);
                    Assert.That(sys.IsValid(whitelistSer, validComponentProto), Is.True);
                    Assert.That(sys.IsValid(whitelistSer, ValidComponentProtoId), Is.True);

                    Assert.That(sys.IsValid(whitelistSer, whitelistTestValidTagUid), Is.True);
                    Assert.That(sys.IsValid(whitelistSer, whitelistTestValidTagProto), Is.True);
                    Assert.That(sys.IsValid(whitelistSer, WhitelistTestValidTagProtoId), Is.True);

                    Assert.That(sys.IsValid(whitelistSer, invalidComponentUid), Is.False);
                    Assert.That(sys.IsValid(whitelistSer, invalidComponentProto), Is.False);
                    Assert.That(sys.IsValid(whitelistSer, InvalidComponentProtoId), Is.False);

                    Assert.That(sys.IsValid(whitelistSer, whitelistTestInvalidTagUid), Is.False);
                    Assert.That(sys.IsValid(whitelistSer, whitelistTestInvalidTagProto), Is.False);
                    Assert.That(sys.IsValid(whitelistSer, WhitelistTestInvalidTagProtoId), Is.False);
                });
            });
            await pair.CleanReturnAsync();
        }
    }
}
