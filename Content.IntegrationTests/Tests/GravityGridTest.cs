using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Gravity;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    /// Tests the behavior of GravityGeneratorComponent,
    /// making sure that gravity is applied to the correct grids.
    [TestFixture]
    [TestOf(typeof(GravityGeneratorComponent))]
    public class GravityGridTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();

            IEntity generator = null;

            IMapGrid grid1 = null;
            IMapGrid grid2 = null;

            // Create grids
            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();

                mapMan.CreateMap(new MapId(1));
                grid1 = mapMan.CreateGrid(new MapId(1));
                grid2 = mapMan.CreateGrid(new MapId(1));

                var entityMan = IoCManager.Resolve<IEntityManager>();

                generator = entityMan.SpawnEntity("GravityGenerator", grid2.ToCoordinates());
                Assert.That(generator.HasComponent<GravityGeneratorComponent>());
                Assert.That(generator.HasComponent<PowerReceiverComponent>());
                var generatorComponent = generator.GetComponent<GravityGeneratorComponent>();
                var powerComponent = generator.GetComponent<PowerReceiverComponent>();
                Assert.That(generatorComponent.Status, Is.EqualTo(GravityGeneratorStatus.Unpowered));
                powerComponent.NeedsPower = false;
            });
            server.RunTicks(1);

            server.Assert(() =>
            {
                var generatorComponent = generator.GetComponent<GravityGeneratorComponent>();

                Assert.That(generatorComponent.Status, Is.EqualTo(GravityGeneratorStatus.On));

                Assert.That(!grid1.HasGravity);
                Assert.That(grid2.HasGravity);
            });

            await server.WaitIdleAsync();
        }
    }
}
