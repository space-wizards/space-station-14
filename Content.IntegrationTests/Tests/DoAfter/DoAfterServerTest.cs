using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using NUnit.Framework;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.DoAfter
{
    [TestFixture]
    [TestOf(typeof(DoAfterComponent))]
    public class DoAfterServerTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: Dummy
  id: Dummy
  components:
  - type: DoAfter
";

        [Test]
        public async Task TestFinished()
        {
            Task<DoAfterStatus> task = null;
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            // That it finishes successfully
            server.Post(() =>
            {
                var tickTime = 1.0f / IoCManager.Resolve<IGameTiming>().TickRate;
                var mapManager = IoCManager.Resolve<IMapManager>();
                mapManager.CreateNewMapEntity(MapId.Nullspace);
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var mob = entityManager.SpawnEntity("Dummy", MapCoordinates.Nullspace);
                var cancelToken = new CancellationTokenSource();
                var args = new DoAfterEventArgs(mob, tickTime / 2, cancelToken.Token);
                task = EntitySystem.Get<DoAfterSystem>().DoAfter(args);
            });

            await server.WaitRunTicks(1);
            Assert.That(task.Result == DoAfterStatus.Finished);
        }

        [Test]
        public async Task TestCancelled()
        {
            Task<DoAfterStatus> task = null;
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            server.Post(() =>
            {
                var tickTime = 1.0f / IoCManager.Resolve<IGameTiming>().TickRate;
                var mapManager = IoCManager.Resolve<IMapManager>();
                mapManager.CreateNewMapEntity(MapId.Nullspace);
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var mob = entityManager.SpawnEntity("Dummy", MapCoordinates.Nullspace);
                var cancelToken = new CancellationTokenSource();
                var args = new DoAfterEventArgs(mob, tickTime * 2, cancelToken.Token);
                task = EntitySystem.Get<DoAfterSystem>().DoAfter(args);
                cancelToken.Cancel();
            });

            await server.WaitRunTicks(3);
            Assert.That(task.Result == DoAfterStatus.Cancelled, $"Result was {task.Result}");
        }
    }
}
