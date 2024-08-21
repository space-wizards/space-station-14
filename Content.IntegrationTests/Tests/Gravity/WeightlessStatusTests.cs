using Content.Server.Gravity;
using Content.Shared.Alert;
using Content.Shared.Gravity;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Gravity
{
    [TestFixture]
    [TestOf(typeof(GravitySystem))]
    [TestOf(typeof(GravityGeneratorComponent))]
    public sealed class WeightlessStatusTests
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: HumanWeightlessDummy
  id: HumanWeightlessDummy
  components:
  - type: Alerts
  - type: Physics
    bodyType: Dynamic

- type: entity
  name: WeightlessGravityGeneratorDummy
  id: WeightlessGravityGeneratorDummy
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
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var alertsSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<AlertsSystem>();
            var weightlessAlert = SharedGravitySystem.WeightlessAlert;

            EntityUid human = default;

            var testMap = await pair.CreateTestMap();

            await server.WaitAssertion(() =>
            {
                human = entityManager.SpawnEntity("HumanWeightlessDummy", testMap.GridCoords);

                Assert.That(entityManager.TryGetComponent(human, out AlertsComponent alerts));
            });

            // Let WeightlessSystem and GravitySystem tick
            await pair.RunTicksSync(10);
            var generatorUid = EntityUid.Invalid;
            await server.WaitAssertion(() =>
            {
                // No gravity without a gravity generator
                Assert.That(alertsSystem.IsShowingAlert(human, weightlessAlert));

                generatorUid = entityManager.SpawnEntity("WeightlessGravityGeneratorDummy", entityManager.GetComponent<TransformComponent>(human).Coordinates);
            });

            // Let WeightlessSystem and GravitySystem tick
            await pair.RunTicksSync(10);

            await server.WaitAssertion(() =>
            {
                Assert.That(alertsSystem.IsShowingAlert(human, weightlessAlert), Is.False);

                // This should kill gravity
                entityManager.DeleteEntity(generatorUid);
            });

            await pair.RunTicksSync(10);

            await server.WaitAssertion(() =>
            {
                Assert.That(alertsSystem.IsShowingAlert(human, weightlessAlert));
            });

            await pair.RunTicksSync(10);

            await pair.CleanReturnAsync();
        }
    }
}
