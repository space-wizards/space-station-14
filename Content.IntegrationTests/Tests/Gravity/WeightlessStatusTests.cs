using System.Threading.Tasks;
using Content.Server.Gravity;
using Content.Server.Gravity.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Gravity
{
    [TestFixture]
    [TestOf(typeof(WeightlessSystem))]
    [TestOf(typeof(GravityGeneratorComponent))]
    public sealed class WeightlessStatusTests : ContentIntegrationTest
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
            var alertsSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<AlertsSystem>();

            EntityUid human = default;

            await server.WaitAssertion(() =>
            {
                var grid = GetMainGrid(mapManager);
                var coordinates = grid.ToCoordinates();
                human = entityManager.SpawnEntity("HumanDummy", coordinates);

                Assert.True(entityManager.TryGetComponent(human, out AlertsComponent alerts));
            });

            // Let WeightlessSystem and GravitySystem tick
            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                // No gravity without a gravity generator
                Assert.True(alertsSystem.IsShowingAlert(human, AlertType.Weightless));

                entityManager.SpawnEntity("GravityGeneratorDummy", entityManager.GetComponent<TransformComponent>(human).Coordinates);
            });

            // Let WeightlessSystem and GravitySystem tick
            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.False(alertsSystem.IsShowingAlert(human, AlertType.Weightless));

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
