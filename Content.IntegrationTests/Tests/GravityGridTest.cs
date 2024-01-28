using Content.Server.Gravity;
using Content.Server.Power.Components;
using Content.Shared.Gravity;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    /// Tests the behavior of GravityGeneratorComponent,
    /// making sure that gravity is applied to the correct grids.
    [TestFixture]
    [TestOf(typeof(GravityGeneratorComponent))]
    public sealed class GravityGridTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: GridGravityGeneratorDummy
  id: GridGravityGeneratorDummy
  components:
  - type: GravityGenerator
    chargeRate: 1000000000 # Set this really high so it discharges in a single tick.
    activePower: 500
  - type: ApcPowerReceiver
  - type: UserInterface
";
        [Test]
        public async Task Test()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var testMap = await pair.CreateTestMap();

            EntityUid generator = default;
            var entityMan = server.ResolveDependency<IEntityManager>();
            var mapMan = server.ResolveDependency<IMapManager>();
            var mapSys = entityMan.System<SharedMapSystem>();

            MapGridComponent grid1 = null;
            MapGridComponent grid2 = null;
            EntityUid grid1Entity = default!;
            EntityUid grid2Entity = default!;

            // Create grids
            await server.WaitAssertion(() =>
            {
                var mapId = testMap.MapId;
                grid1 = mapMan.CreateGrid(mapId);
                grid2 = mapMan.CreateGrid(mapId);
                grid1Entity = grid1.Owner;
                grid2Entity = grid2.Owner;

                mapSys.SetTile(grid1Entity, grid1, Vector2i.Zero, new Tile(1));
                mapSys.SetTile(grid2Entity, grid2, Vector2i.Zero, new Tile(1));

                generator = entityMan.SpawnEntity("GridGravityGeneratorDummy", new EntityCoordinates(grid1Entity, 0.5f, 0.5f));
                Assert.Multiple(() =>
                {
                    Assert.That(entityMan.HasComponent<GravityGeneratorComponent>(generator));
                    Assert.That(entityMan.HasComponent<ApcPowerReceiverComponent>(generator));
                });

                var powerComponent = entityMan.GetComponent<ApcPowerReceiverComponent>(generator);
                powerComponent.NeedsPower = false;
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                var generatorComponent = entityMan.GetComponent<GravityGeneratorComponent>(generator);
                var powerComponent = entityMan.GetComponent<ApcPowerReceiverComponent>(generator);

                Assert.Multiple(() =>
                {
                    Assert.That(generatorComponent.GravityActive, Is.True);
                    Assert.That(!entityMan.GetComponent<GravityComponent>(grid1Entity).EnabledVV);
                    Assert.That(entityMan.GetComponent<GravityComponent>(grid2Entity).EnabledVV);
                });

                // Re-enable needs power so it turns off again.
                // Charge rate is ridiculously high so it finishes in one tick.
                powerComponent.NeedsPower = true;
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                var generatorComponent = entityMan.GetComponent<GravityGeneratorComponent>(generator);

                Assert.Multiple(() =>
                {
                    Assert.That(generatorComponent.GravityActive, Is.False);
                    Assert.That(entityMan.GetComponent<GravityComponent>(grid2Entity).EnabledVV, Is.False);
                });
            });

            await pair.CleanReturnAsync();
        }
    }
}
