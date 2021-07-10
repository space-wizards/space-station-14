using System.Threading.Tasks;
using Content.Server.Gravity;
using Content.Server.Power.Components;
using Content.Shared.Coordinates;
using Content.Shared.Gravity;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests
{
    /// Tests the behavior of GravityGeneratorComponent,
    /// making sure that gravity is applied to the correct grids.
    [TestFixture]
    [TestOf(typeof(GravityGeneratorComponent))]
    public class GravityGridTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: GravityGeneratorDummy
  id: GravityGeneratorDummy
  components:
  - type: GravityGenerator
  - type: ApcPowerReceiver
";
        [Test]
        public async Task Test()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

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

                generator = entityMan.SpawnEntity("GravityGeneratorDummy", grid2.ToCoordinates());
                Assert.That(generator.HasComponent<GravityGeneratorComponent>());
                Assert.That(generator.HasComponent<ApcPowerReceiverComponent>());
                var generatorComponent = generator.GetComponent<GravityGeneratorComponent>();
                var powerComponent = generator.GetComponent<ApcPowerReceiverComponent>();
                Assert.That(generatorComponent.Status, Is.EqualTo(GravityGeneratorStatus.Unpowered));
                powerComponent.NeedsPower = false;
            });
            server.RunTicks(1);

            server.Assert(() =>
            {
                var generatorComponent = generator.GetComponent<GravityGeneratorComponent>();

                Assert.That(generatorComponent.Status, Is.EqualTo(GravityGeneratorStatus.On));

                var entityMan = IoCManager.Resolve<IEntityManager>();
                var grid1Entity = entityMan.GetEntity(grid1.GridEntityId);
                var grid2Entity = entityMan.GetEntity(grid2.GridEntityId);

                Assert.That(!grid1Entity.GetComponent<GravityComponent>().Enabled);
                Assert.That(grid2Entity.GetComponent<GravityComponent>().Enabled);
            });

            await server.WaitIdleAsync();
        }
    }
}
