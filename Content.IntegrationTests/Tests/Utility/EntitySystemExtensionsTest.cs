#nullable enable
using System.Threading.Tasks;
using Content.Shared.Physics;
using Content.Shared.Spawning;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.IntegrationTests.Tests.Utility
{
    [TestFixture]
    [TestOf(typeof(EntitySystemExtensions))]
    public class EntitySystemExtensionsTest : ContentIntegrationTest
    {
        private const string BlockerDummyId = "BlockerDummy";

        private static readonly string Prototypes = $@"
- type: entity
  id: {BlockerDummyId}
  name: {BlockerDummyId}
  components:
  - type: Physics
  - type: Fixtures
    fixtures:
    - shape:
        !type:PhysShapeAabb
          bounds: ""-0.49,-0.49,0.49,0.49""
      mask:
      - Impassable
";

        [Test]
        public async Task Test()
        {
            var serverOptions = new ServerContentIntegrationOption {ExtraPrototypes = Prototypes};
            var server = StartServer(serverOptions);

            await server.WaitIdleAsync();

            var sMapManager = server.ResolveDependency<IMapManager>();
            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var broady = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SharedBroadphaseSystem>();

            await server.WaitAssertion(() =>
            {
                var grid = GetMainGrid(sMapManager);
                var gridEnt = grid.GridEntityId;
                var gridPos = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt).WorldPosition;
                var entityCoordinates = GetMainEntityCoordinates(sMapManager);

                // Nothing blocking it, only entity is the grid
                Assert.NotNull(sEntityManager.SpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.Impassable));
                Assert.True(sEntityManager.TrySpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.Impassable, out var entity));
                Assert.NotNull(entity);

                var mapId = GetMainMapId(sMapManager);
                var mapCoordinates = new MapCoordinates(gridPos.X, gridPos.Y, mapId);

                // Nothing blocking it, only entity is the grid
                Assert.NotNull(sEntityManager.SpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.Impassable));
                Assert.True(sEntityManager.TrySpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.Impassable, out entity));
                Assert.NotNull(entity);

                // Spawn a blocker with an Impassable mask
                sEntityManager.SpawnEntity(BlockerDummyId, entityCoordinates);
                broady.Update(0.016f);

                // Cannot spawn something with an Impassable layer
                Assert.Null(sEntityManager.SpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.Impassable));
                Assert.False(sEntityManager.TrySpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.Impassable, out entity));
                Assert.Null(entity);

                Assert.Null(sEntityManager.SpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.Impassable));
                Assert.False(sEntityManager.TrySpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.Impassable, out entity));
                Assert.Null(entity);

                // Other layers are fine
                Assert.NotNull(sEntityManager.SpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.MobImpassable));
                Assert.True(sEntityManager.TrySpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.MobImpassable, out entity));
                Assert.NotNull(entity);

                Assert.NotNull(sEntityManager.SpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.MobImpassable));
                Assert.True(sEntityManager.TrySpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.MobImpassable, out entity));
                Assert.NotNull(entity);
            });
        }
    }
}
