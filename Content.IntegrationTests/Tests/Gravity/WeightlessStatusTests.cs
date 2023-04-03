using System.Threading.Tasks;
using Content.Server.Gravity;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Gravity
{
    [TestFixture]
    [TestOf(typeof(GravitySystem))]
    [TestOf(typeof(GravityGeneratorComponent))]
    public sealed class WeightlessStatusTests
    {
        private const string Prototypes = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Alerts
  - type: Physics
    bodyType: Dynamic

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
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var alertsSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<AlertsSystem>();

            EntityUid human = default;

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                human = entityManager.SpawnEntity("HumanDummy", testMap.GridCoords);

                Assert.True(entityManager.TryGetComponent(human, out AlertsComponent alerts));
            });

            // Let WeightlessSystem and GravitySystem tick
            await PoolManager.RunTicksSync(pairTracker.Pair, 10);
            var generatorUid = EntityUid.Invalid;
            await server.WaitAssertion(() =>
            {
                // No gravity without a gravity generator
                Assert.True(alertsSystem.IsShowingAlert(human, AlertType.Weightless));

                generatorUid = entityManager.SpawnEntity("GravityGeneratorDummy", entityManager.GetComponent<TransformComponent>(human).Coordinates);
            });

            // Let WeightlessSystem and GravitySystem tick
            await PoolManager.RunTicksSync(pairTracker.Pair, 10);

            await server.WaitAssertion(() =>
            {
                Assert.False(alertsSystem.IsShowingAlert(human, AlertType.Weightless));

                // This should kill gravity
                entityManager.DeleteEntity(generatorUid);
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 10);

            await server.WaitAssertion(() =>
            {
                Assert.True(alertsSystem.IsShowingAlert(human, AlertType.Weightless));
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 10);

            await pairTracker.CleanReturnAsync();
        }
    }
}
