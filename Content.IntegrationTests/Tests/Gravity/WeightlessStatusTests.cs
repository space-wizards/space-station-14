using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Gravity;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Gravity
{
    [TestFixture]
    [TestOf(typeof(WeightlessSystem))]
    [TestOf(typeof(GravityGeneratorComponent))]
    public class WeightlessStatusTests : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Alerts
";
        [Test]
        public async Task WeightlessStatusTest()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServer(options);

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            IEntity human = null;
            SharedAlertsComponent alerts = null;

            await server.WaitAssertion(() =>
            {
                var mapId = new MapId(1);
                var gridId = new GridId(1);

                if (!mapManager.TryGetGrid(gridId, out var grid))
                {
                    grid = mapManager.CreateGrid(mapId, gridId);
                }

                var coordinates = grid.ToCoordinates();
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
                Assert.True(alerts.IsShowingAlert(AlertType.Weightless));
            });
        }
    }
}
