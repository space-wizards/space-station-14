using System.Threading.Tasks;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Body
{
    [TestFixture]
    [TestOf(typeof(LungSystem))]
    public sealed class LungTest
    {
        private const string Prototypes = @"
- type: entity
  name: HumanBodyDummy
  id: HumanBodyDummy
  components:
  - type: SolutionContainerManager
  - type: Body
    prototype: Human
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
            // --- Setup
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
                {NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entityManager.System<MapLoaderSystem>();
            RespiratorSystem respSys = default;
            MetabolizerSystem metaSys = default;

            MapId mapId;
            EntityUid? grid = null;
            BodyComponent body = default;
            EntityUid human = default;
            GridAtmosphereComponent relevantAtmos = default;
            float startingMoles = 0.0f;

            var testMapName = "Maps/Test/Breathing/3by3-20oxy-80nit.yml";

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                grid = mapLoader.LoadGrid(mapId, testMapName);
            });

            Assert.NotNull(grid, $"Test blueprint {testMapName} not found.");

            float GetMapMoles()
            {
                var totalMapMoles = 0.0f;
                foreach (var tile in relevantAtmos.Tiles.Values)
                {
                    totalMapMoles += tile.Air?.TotalMoles ?? 0.0f;
                }

                return totalMapMoles;
            }

            await server.WaitAssertion(() =>
            {
                var coords = new Vector2(0.5f, -1f);
                var coordinates = new EntityCoordinates(grid.Value, coords);
                human = entityManager.SpawnEntity("HumanBodyDummy", coordinates);
                respSys = EntitySystem.Get<RespiratorSystem>();
                metaSys = EntitySystem.Get<MetabolizerSystem>();
                relevantAtmos = entityManager.GetComponent<GridAtmosphereComponent>(grid.Value);
                startingMoles = GetMapMoles();

                Assert.True(entityManager.TryGetComponent(human, out body));
                Assert.True(entityManager.HasComponent<RespiratorComponent>(human));
            });

            // --- End setup

            var inhaleCycles = 100;
            for (var i = 0; i < inhaleCycles; i++)
            {
                await server.WaitAssertion(() =>
                {
                    // inhale
                    respSys.Update(2.0f);
                    Assert.That(GetMapMoles(), Is.LessThan(startingMoles));

                    // metabolize + exhale
                    metaSys.Update(1.0f);
                    metaSys.Update(1.0f);
                    respSys.Update(2.0f);
                    Assert.That(GetMapMoles(), Is.EqualTo(startingMoles).Within(0.0001));
                });
            }

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task NoSuffocationTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
                {NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entityManager.System<MapLoaderSystem>();

            MapId mapId;
            EntityUid? grid = null;
            RespiratorComponent respirator = null;
            EntityUid human = default;

            var testMapName = "Maps/Test/Breathing/3by3-20oxy-80nit.yml";

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                grid = mapLoader.LoadGrid(mapId, testMapName);
            });

            Assert.NotNull(grid, $"Test blueprint {testMapName} not found.");

            await server.WaitAssertion(() =>
            {
                var center = new Vector2(0.5f, -1.5f);
                var coordinates = new EntityCoordinates(grid.Value, center);
                human = entityManager.SpawnEntity("HumanBodyDummy", coordinates);

                Assert.True(entityManager.HasComponent<BodyComponent>(human));
                Assert.True(entityManager.TryGetComponent(human, out respirator));
                Assert.False(respirator.SuffocationCycles > respirator.SuffocationCycleThreshold);
            });

            var increment = 10;

            for (var tick = 0; tick < 600; tick += increment)
            {
                await server.WaitRunTicks(increment);
                await server.WaitAssertion(() =>
                {
                    Assert.False(respirator.SuffocationCycles > respirator.SuffocationCycleThreshold,
                        $"Entity {entityManager.GetComponent<MetaDataComponent>(human).EntityName} is suffocating on tick {tick}");
                });
            }

            await pairTracker.CleanReturnAsync();
        }
    }
}
