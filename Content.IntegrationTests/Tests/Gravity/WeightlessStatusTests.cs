using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Gravity;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Gravity;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Gravity
{
    [TestFixture]
    [TestOf(typeof(WeightlessSystem))]
    [TestOf(typeof(GravityGeneratorComponent))]
    public class WeightlessStatusTests : ContentIntegrationTest
    {
        private const string PROTOTYPES = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: AlertsUI
";
        [Test]
        public async Task WeightlessStatusTest()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = PROTOTYPES};
            var server = StartServer(options);

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            IEntity human = null;
            SharedAlertsComponent alerts = null;

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();

                pauseManager.AddUninitializedMap(mapId);

                var gridId = new GridId(1);

                if (!mapManager.TryGetGrid(gridId, out var grid))
                {
                    grid = mapManager.CreateGrid(mapId, gridId);
                }

                var tileDefinition = tileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                var coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                pauseManager.DoMapInitialize(mapId);

                human = entityManager.SpawnEntity("HumanDummy", coordinates);

                Assert.True(human.TryGetComponent(out alerts));
            });

            // Let WeightlessSystem and GravitySystem tick
            await server.WaitRunTicks(1);

            GravityGeneratorComponent gravityGenerator = null;

            await server.WaitAssertion(() =>
            {
                // No gravity without a gravity generator
                Assert.True(alerts.IsShowingAlert(AlertType.Weightless));

                gravityGenerator = human.EnsureComponent<GravityGeneratorComponent>();
            });

            // Let WeightlessSystem and GravitySystem tick
            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.False(alerts.IsShowingAlert(AlertType.Weightless));

                // Disable the gravity generator
                var args = new BreakageEventArgs {Owner = human};
                gravityGenerator.OnBreak(args);
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.False(alerts.IsShowingAlert(AlertType.Weightless));
            });
        }
    }
}
