#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Server.Gravity;
using Content.Shared.Alert;
using Content.Shared.Gravity;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Gravity;

[TestOf(typeof(GravitySystem))]
[TestOf(typeof(GravityGeneratorComponent))]
public sealed class WeightlessStatusTests : GameTest
{
    private const string HumanWeightlessDummy = "HumanWeightlessDummy";
    private const string WeightlessGravityGeneratorDummy = "WeightlessGravityGeneratorDummy";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {HumanWeightlessDummy}
  id: {HumanWeightlessDummy}
  components:
  - type: Alerts
  - type: Physics
    bodyType: Dynamic
  - type: GravityAffected

- type: entity
  name: {WeightlessGravityGeneratorDummy}
  id: {WeightlessGravityGeneratorDummy}
  components:
  - type: GravityGenerator
  - type: PowerCharge
    windowTitle: gravity-generator-window-title
    idlePower: 50
    chargeRate: 1000000000 # Set this really high so it discharges in a single tick.
    activePower: 500
  - type: ApcPowerReceiver
    needsPower: false
  - type: UserInterface
";

    [SidedDependency(Side.Server)] private AlertsSystem _sAlertsSystem = default!;

    [Test]
    public async Task WeightlessStatusTest()
    {
        var weightlessAlert = SharedGravitySystem.WeightlessAlert;
        EntityUid human = default;

        await Pair.CreateTestMap();
        Assert.That(TestMap, Is.Not.Null);

        await Server.WaitAssertion(() =>
        {
            human = SSpawnAtPosition(HumanWeightlessDummy, TestMap.GridCoords);

            Assert.That(human, Has.Comp<AlertsComponent>(Server));
        });

        // Let WeightlessSystem and GravitySystem tick
        await RunTicksSync(10);
        var generatorUid = EntityUid.Invalid;
        await Server.WaitAssertion(() =>
        {
            // No gravity without a gravity generator
            Assert.That(_sAlertsSystem.IsShowingAlert(human, weightlessAlert));

            generatorUid = SSpawnAtPosition(WeightlessGravityGeneratorDummy, SComp<TransformComponent>(human).Coordinates);
        });

        // Let WeightlessSystem and GravitySystem tick
        await RunTicksSync(10);

        await Server.WaitAssertion(() =>
        {
            Assert.That(_sAlertsSystem.IsShowingAlert(human, weightlessAlert), Is.False);

            // This should kill gravity
            SDeleteNow(generatorUid);
        });

        await RunTicksSync(10);

        await Server.WaitAssertion(() =>
        {
            Assert.That(_sAlertsSystem.IsShowingAlert(human, weightlessAlert));
        });

        await RunTicksSync(10);
    }
}
