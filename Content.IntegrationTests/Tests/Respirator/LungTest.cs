using System.Numerics;
using Content.IntegrationTests.Tests.Atmos;
using Content.Server.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Respirator;

[TestFixture]
[TestOf(typeof(LungSystem))]
public sealed class LungTest : AtmosTest
{
    protected override ResPath? TestMapPath => new ("Maps/Test/Atmospherics/load_atmos_test_room.yml");

    private readonly Vector2 _gridCenter = new (2.5f, 2.5f);

    private readonly EntProtoId _humanLungDummy = new("HumanLungDummy");

    private Entity<RespiratorComponent> _dummy;

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  name: HumanLungDummy
  id: HumanLungDummy
  components:
  - type: SolutionContainerManager
  - type: Body
  - type: EntityTableContainerFill
    containers:
      body_organs: !type:AllSelector
        children:
        - id: OrganHumanLungs
  - type: MobState
    allowedStates:
      - Alive
  - type: Damageable
  - type: ThermalRegulator
    metabolismHeat: 5000
    radiatedHeat: 400
    implicitHeatRegulation: 5000
    sweatHeatRegulation: 5000
    shiveringHeatRegulation: 5000
    normalBodyTemperature: 310.15
    thermalRegulationTemperatureThreshold: 25
  - type: Respirator
    damage:
      types:
        Asphyxiation: 1.5
    damageRecovery:
      types:
        Asphyxiation: -1.5
";

    [Test]
    public async Task AirConsistencyTest()
    {
        await Pair.CreateTestMap();

        var startingMoles = 0.0f;

        // --- Setup
        await Server.WaitPost(() =>
        {
            SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);
            startingMoles = GetMapMoles();
        });

        float GetMapMoles()
        {
            var totalMapMoles = 0.0f;
            foreach (var tile in ProcessEnt.Comp1.Tiles.Values)
            {
                totalMapMoles += tile.Air?.TotalMoles ?? 0.0f;
            }

            return totalMapMoles;
        }

        await Server.WaitAssertion(() =>
        {
            var human = SSpawnAtPosition(_humanLungDummy, new EntityCoordinates(ProcessEnt, _gridCenter));

            Assert.That(SEntMan.TryGetComponent<RespiratorComponent>(human, out var resp), Is.True);

            _dummy = (human, resp);
        });

        // --- End setup

        var inhaleCycles = 100;
        for (var i = 0; i < inhaleCycles; i++)
        {
            // Breathe in
            await PoolManager.WaitUntil(Server, () => _dummy.Comp.Status == RespiratorStatus.Exhaling);
            Assert.That(
                GetMapMoles(),
                Is.LessThan(startingMoles),
                "Did not inhale in any gas"
            );

            // Breathe out
            await PoolManager.WaitUntil(Server, () => _dummy.Comp.Status == RespiratorStatus.Inhaling);
            Assert.That(
                GetMapMoles(),
                Is.EqualTo(startingMoles).Within(0.0002),
                "Did not exhale as much gas as was inhaled"
            );
        }
    }

    [Test]
    public async Task NoSuffocationTest()
    {
        await Pair.CreateTestMap();

        await Server.WaitPost(() =>
        {
            SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);
        });

        await Server.WaitAssertion(() =>
        {
            var human = SSpawnAtPosition(_humanLungDummy, new EntityCoordinates(ProcessEnt, _gridCenter));

            var mixture = SAtmos.GetContainingMixture(human);
            Assert.Multiple(() =>
            {
                Assert.That(mixture!.TotalMoles, Is.GreaterThan(0));
                Assert.That(SEntMan.TryGetComponent<RespiratorComponent>(human, out var respirator), Is.True);
                Assert.That(respirator!.SuffocationCycles, Is.LessThanOrEqualTo(respirator.SuffocationCycleThreshold));
                _dummy = (human, respirator);
            });
        });

        var increment = 10;

        // 20 seconds
        var total = 20 * Server.CfgMan.GetCVar(CVars.NetTickrate);

        for (var tick = 0; tick < total; tick += increment)
        {
            await Server.WaitRunTicks(increment);
            await Server.WaitAssertion(() =>
            {
                Assert.That(_dummy.Comp.SuffocationCycles,
                    Is.LessThanOrEqualTo(_dummy.Comp.SuffocationCycleThreshold),
                    $"Entity {SToPrettyString(_dummy)} is suffocating on tick {tick}");
            });
        }
    }
}
