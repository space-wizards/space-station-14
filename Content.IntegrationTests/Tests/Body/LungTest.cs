using System.Threading.Tasks;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using NUnit.Framework;
using Robust.Server.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Body
{
    [TestFixture]
    [TestOf(typeof(LungSystem))]
    public class LungTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: HumanBodyDummy
  id: HumanBodyDummy
  components:
  - type: SolutionContainerManager
  - type: Body
    template: HumanoidTemplate
    preset: HumanPreset
    centerSlot: torso
  - type: ThermalRegulator
    metabolismHeat: 5000
    radiatedHeat: 400
    implicitHeatRegulation: 5000
    sweatHeatRegulation: 5000
    shiveringHeatRegulation: 5000
    normalBodyTemperature: 310.15
    thermalRegulationTemperatureThreshold: 25
  - type: Respirator
";

        [Test]
        public async Task AirConsistencyTest()
        {
            var options = new ServerContentIntegrationOption{ExtraPrototypes = Prototypes};
            var server = StartServer(options);

            server.Assert(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                var mapId = mapManager.CreateMap();

                var entityManager = IoCManager.Resolve<IEntityManager>();

                var human = entityManager.SpawnEntity("HumanBodyDummy", new MapCoordinates(Vector2.Zero, mapId));

                var bodySys = EntitySystem.Get<BodySystem>();
                var lungSys = EntitySystem.Get<LungSystem>();
                var respSys = EntitySystem.Get<RespiratorSystem>();

            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task NoSuffocationTest()
        {
            var options = new ServerContentIntegrationOption{ExtraPrototypes = Prototypes};
            var server = StartServer(options);

            await server.WaitIdleAsync();

            var mapLoader = server.ResolveDependency<IMapLoader>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            MapId mapId;
            IMapGrid grid = null;
            RespiratorComponent respirator = null;
            EntityUid human = default;

            var testMapName = "Maps/Test/Breathing/3by3-20oxy-80nit.yml";

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                grid = mapLoader.LoadBlueprint(mapId, testMapName);
            });

            Assert.NotNull(grid, $"Test blueprint {testMapName} not found.");

            await server.WaitAssertion(() =>
            {
                var center = new Vector2(0.5f, -1.5f);
                var coordinates = new EntityCoordinates(grid.GridEntityId, center);
                human = entityManager.SpawnEntity("HumanBodyDummy", coordinates);

                Assert.True(entityManager.HasComponent<SharedBodyComponent>(human));
                Assert.True(entityManager.TryGetComponent(human, out respirator));
                Assert.False(respirator.Suffocating);
            });

            var increment = 10;

            for (var tick = 0; tick < 600; tick += increment)
            {
                await server.WaitRunTicks(increment);
                await server.WaitAssertion(() =>
                {
                    Assert.False(respirator.Suffocating, $"Entity {entityManager.GetComponent<MetaDataComponent>(human).EntityName} is suffocating on tick {tick}");
                });
            }

            await server.WaitIdleAsync();
        }
    }
}
