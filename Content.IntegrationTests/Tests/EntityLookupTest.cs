using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.EntityLookup;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(SharedEntityLookupSystem))]
    public class EntityLookupSystemTest : ContentIntegrationTest
    {
        private const string PROTOTYPES = @"
- type: entity
  name: mob dummy
  id: MobDummy
  components:
  - type: Physics
  shapes:
    - !type:PhysShapeAabb
      layer:
      - Impassable
      - MobImpassable
";

        [Test]
        public async Task LookupTest()
        {
            var serverOptions = new ServerIntegrationOptions{ExtraPrototypes = PROTOTYPES};

            var (client, server) = await StartConnectedServerClientPair(serverOptions: serverOptions);

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var systemManager = server.ResolveDependency<IEntitySystemManager>();
            var lookupSystem = systemManager.GetEntitySystem<SharedEntityLookupSystem>();

            MapId mapId = default;
            IMapGrid grid = default;
            Box2 worldBox = default;

            await server.WaitAssertion(() =>
            {
                mapId = mapManager.CreateMap();
                grid = mapManager.CreateGrid(mapId);
                worldBox = new Box2(0.001f, 0.001f, 0.999f, 0.999f);

                // Check no chunks
                var chunkCount = lookupSystem.GetChunksInRange(mapId, worldBox, grid.Index).Count;
                Assert.That(chunkCount == 0, $"Expected 0 chunks but found {chunkCount}");

                // Check overlap of multiple chunks
                var dummy = entityManager.SpawnEntity("MobDummy", new MapCoordinates(new Vector2(0f, 0f), mapId));
                chunkCount = lookupSystem.GetChunksInRange(mapId, worldBox, grid.Index).Count;
                Assert.That(chunkCount == 4, $"Expected 4 chunks but found {chunkCount}");

                dummy.Delete();
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                // Check entity removed
                var chunkCount = lookupSystem.GetChunksInRange(mapId, worldBox, grid.Index).Count;
                Assert.That(chunkCount == 0, $"Expected 0 chunks but found {chunkCount}");
            });
        }
    }
}
