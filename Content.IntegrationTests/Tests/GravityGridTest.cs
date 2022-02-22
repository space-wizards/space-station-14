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
    public sealed class GravityGridTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: GravityGeneratorDummy
  id: GravityGeneratorDummy
  components:
  - type: GravityGenerator
    chargeRate: 1000000000 # Set this really high so it discharges in a single tick.
    activePower: 500
  - type: ApcPowerReceiver
  - type: UserInterface
";
        [Test]
        public async Task Test()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServer(options);

            await server.WaitIdleAsync();

            EntityUid generator = default;
            var entityMan = server.ResolveDependency<IEntityManager>();

            IMapGrid grid1 = null;
            IMapGrid grid2 = null;

            // Create grids
            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();

                var mapId = GetMainMapId(mapMan);
                grid1 = mapMan.CreateGrid(mapId);
                grid2 = mapMan.CreateGrid(mapId);

                generator = entityMan.SpawnEntity("GravityGeneratorDummy", grid2.ToCoordinates());
                Assert.That(entityMan.HasComponent<GravityGeneratorComponent>(generator));
                Assert.That(entityMan.HasComponent<ApcPowerReceiverComponent>(generator));

                var powerComponent = entityMan.GetComponent<ApcPowerReceiverComponent>(generator);
                powerComponent.NeedsPower = false;
            });
            server.RunTicks(1);

            server.Assert(() =>
            {
                var generatorComponent = entityMan.GetComponent<GravityGeneratorComponent>(generator);
                var powerComponent = entityMan.GetComponent<ApcPowerReceiverComponent>(generator);

                Assert.That(generatorComponent.GravityActive, Is.True);

                var grid1Entity = grid1.GridEntityId;
                var grid2Entity = grid2.GridEntityId;

                Assert.That(!entityMan.GetComponent<GravityComponent>(grid1Entity).Enabled);
                Assert.That(entityMan.GetComponent<GravityComponent>(grid2Entity).Enabled);

                // Re-enable needs power so it turns off again.
                // Charge rate is ridiculously high so it finishes in one tick.
                powerComponent.NeedsPower = true;
            });
            server.RunTicks(1);
            server.Assert(() =>
            {
                var generatorComponent = entityMan.GetComponent<GravityGeneratorComponent>(generator);

                Assert.That(generatorComponent.GravityActive, Is.False);

                var grid2Entity = grid2.GridEntityId;

                Assert.That(entityMan.GetComponent<GravityComponent>(grid2Entity).Enabled, Is.False);
            });

            await server.WaitIdleAsync();
        }
    }
}
