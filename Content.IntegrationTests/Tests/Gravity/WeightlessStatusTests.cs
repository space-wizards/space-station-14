using System.Threading.Tasks;
using Content.Server.Gravity;
using Content.Server.Gravity.EntitySystems;
using Content.Shared.Acts;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using Content.Shared.Gravity;
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
- type: entity
  name: GravityGeneratorDummy
  id: GravityGeneratorDummy
  components:
  - type: GravityGenerator
    chargeRate: 1000000000 # Set this really high so it discharges in a single tick.
    activePower: 500
  - type: ApcPowerReceiver
    needsPower: false
  - type: UserInterface
";
        [Test]
        public async Task WeightlessStatusTest()
        {
            var options = new ServerContentIntegrationOption {ExtraPrototypes = Prototypes};
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

            await server.WaitAssertion(() =>
            {
                // No gravity without a gravity generator
                Assert.True(alerts.IsShowingAlert(AlertType.Weightless));

                entityManager.SpawnEntity("GravityGeneratorDummy", human.Transform.Coordinates);
            });

            // Let WeightlessSystem and GravitySystem tick
            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.False(alerts.IsShowingAlert(AlertType.Weightless));

                // TODO: Re-add gravity generator breaking when Vera is done with construction stuff.
                /*
                // Disable the gravity generator
                var args = new BreakageEventArgs {Owner = human};
                // gravityGenerator.OnBreak(args);
                */
            });

            /*await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.True(alerts.IsShowingAlert(AlertType.Weightless));
            });*/
        }
    }
}
