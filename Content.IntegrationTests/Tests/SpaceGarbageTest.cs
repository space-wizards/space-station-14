using NUnit.Framework;
using System.Threading.Tasks;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Content.Server.Shuttles.Systems;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(SpaceGarbageSystem))]

    public sealed class SpaceGarbageTest
    {
        private const string Prototypes = @"
- type: entity
  id: TestGrille
  placement:
    mode: SnapgridCenter
  components:
  - type: Fixtures
    fixtures:
    - shape: !type:PhysShapeCircle
        position: 0,0
        radius: 1
      mask:
      - AllMask
      layer:
      - AllMask
  - type: Physics
    bodyType: Static
  - type: Transform
    anchored: true

- type: entity
  id: TestShard
  components:
  - type: CollisionWake
  - type: Fixtures
    fixtures:
    - shape: !type:PhysShapeCircle
        position: 0,0
        radius: 0.5
      layer:
      - AllMask
      mask:
      - AllMask
  - type: Physics
    bodyType: Dynamic
  - type: SpaceGarbage
";

        [Test]
        public async Task TestSpaceGarbageDoesNotDeleteOnSpawn()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);
            var coordinates = testMap.GridCoords;

            EntityUid grille = default;
            EntityUid shard = default;

            await server.WaitAssertion(() =>
            {
                // First, do a bare minimum test of the collision involved.
                grille = entityManager.SpawnEntity("TestGrille", coordinates);
                shard = entityManager.SpawnEntity("TestShard", entityManager.GetComponent<TransformComponent>(grille).MapPosition);
            });
            await server.WaitRunTicks(15);
            await server.WaitAssertion(() =>
            {
                Assert.IsFalse(entityManager.Deleted(shard),
                    "Space garbage entity was deleted after being spawned on top of another entity on the same grid.");

                entityManager.DeleteEntity(grille);
                entityManager.DeleteEntity(shard);
            });
            await server.WaitRunTicks(15);
            await server.WaitAssertion(() =>
            {
                // Next, try the actual entities used in game.
                grille = entityManager.SpawnEntity("Grille", coordinates);
                shard = entityManager.SpawnEntity("ShardGlass", entityManager.GetComponent<TransformComponent>(grille).MapPosition);
            });
            await server.WaitRunTicks(15);
            await server.WaitAssertion(() =>
            {
                Assert.IsFalse(entityManager.Deleted(shard),
                    "Real glass shard entity was deleted after being spawned on top of a real grille entity on the same grid.");

                mapManager.DeleteMap(testMap.MapId);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
