using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Body.Respiratory;
using Content.Server.GameObjects.Components.Metabolism;
using Content.Shared.Atmos;
using NUnit.Framework;
using Robust.Server.Interfaces.Maps;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(LungComponent))]
    public class LungTest : ContentIntegrationTest
    {
        [Test]
        public async Task AirConsistencyTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                var human = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);
                var lung = human.GetComponent<LungComponent>();
                var gas = new GasMixture(1);

                var originalOxygen = 2;
                var originalNitrogen = 8;
                var breathedPercentage = Atmospherics.BreathPercentage;

                gas.AdjustMoles(Gas.Oxygen, originalOxygen);
                gas.AdjustMoles(Gas.Nitrogen, originalNitrogen);

                lung.Inhale(1, gas);

                var lungOxygen = originalOxygen * breathedPercentage;
                var lungNitrogen = originalNitrogen * breathedPercentage;

                Assert.That(lung.Air.GetMoles(Gas.Oxygen), Is.EqualTo(lungOxygen));
                Assert.That(lung.Air.GetMoles(Gas.Nitrogen), Is.EqualTo(lungNitrogen));

                var mixtureOxygen = originalOxygen - lungOxygen;
                var mixtureNitrogen = originalNitrogen - lungNitrogen;

                Assert.That(gas.GetMoles(Gas.Oxygen), Is.EqualTo(mixtureOxygen));
                Assert.That(gas.GetMoles(Gas.Nitrogen), Is.EqualTo(mixtureNitrogen));

                lung.Exhale(1, gas);

                var exhalePercentage = 0.5f;
                var exhaledOxygen = lungOxygen * exhalePercentage;
                var exhaledNitrogen = lungNitrogen * exhalePercentage;

                lungOxygen -= exhaledOxygen;
                lungNitrogen -= exhaledNitrogen;

                Assert.That(lung.Air.GetMoles(Gas.Oxygen), Is.EqualTo(lungOxygen).Within(0.000001f));
                Assert.That(lung.Air.GetMoles(Gas.Nitrogen), Is.EqualTo(lungNitrogen).Within(0.000001f));

                mixtureOxygen += exhaledOxygen;
                mixtureNitrogen += exhaledNitrogen;

                Assert.That(gas.GetMoles(Gas.Oxygen), Is.EqualTo(mixtureOxygen).Within(0.000001f));
                Assert.That(gas.GetMoles(Gas.Nitrogen), Is.EqualTo(mixtureNitrogen).Within(0.000001f));
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task NoSuffocationTest()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();

            var mapLoader = server.ResolveDependency<IMapLoader>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            MapId mapId;
            IMapGrid grid = null;
            LungComponent lung = null;
            MetabolismComponent metabolism = null;
            IEntity human = null;

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
                human = entityManager.SpawnEntity("HumanMob_Content", coordinates);

                Assert.True(human.TryGetComponent(out lung));
                Assert.True(human.TryGetComponent(out metabolism));
                Assert.False(metabolism.Suffocating);
            });

            for (var tick = 0; tick < 600; tick++)
            {
                await server.WaitRunTicks(tick);
                Assert.False(metabolism.Suffocating, $"Entity {human.Name} is suffocating on tick {tick}");
            }

            await server.WaitIdleAsync();
        }
    }
}
