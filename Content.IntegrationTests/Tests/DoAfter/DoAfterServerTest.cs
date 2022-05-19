using System.Threading;
using System.Threading.Tasks;
using Content.Server.DoAfter;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.DoAfter
{
    [TestFixture]
    [TestOf(typeof(DoAfterComponent))]
    public sealed class DoAfterServerTest : ContentIntegrationTest
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
            var server = StartServer(options);

            await server.WaitIdleAsync();
            var gameTiming = server.ResolveDependency<IGameTiming>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var doAfter = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<DoAfterSystem>();

            // That it finishes successfully
            server.Post(() =>
            {
                var tickTime = 1.0f / gameTiming.TickRate;
                var mapId = mapManager.CreateMap();
                var mob = entityManager.SpawnEntity("Dummy", new MapCoordinates(Vector2.Zero, mapId));
                var cancelToken = new CancellationTokenSource();
                var args = new DoAfterEventArgs(mob, tickTime / 2, cancelToken.Token);
                task = doAfter.WaitDoAfter(args);
            });

            await server.WaitRunTicks(1);
            Assert.That(task.Result, Is.EqualTo(DoAfterStatus.Finished));
        }

        [Test]
        public async Task TestCancelled()
        {
            Task<DoAfterStatus> task = null;
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServer(options);
            await server.WaitIdleAsync();

            var gameTiming = server.ResolveDependency<IGameTiming>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var doAfter = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<DoAfterSystem>();

            server.Post(() =>
            {
                var tickTime = 1.0f / gameTiming.TickRate;
                var mapId = mapManager.CreateMap();
                var mob = entityManager.SpawnEntity("Dummy", new MapCoordinates(Vector2.Zero, mapId));
                var cancelToken = new CancellationTokenSource();
                var args = new DoAfterEventArgs(mob, tickTime * 2, cancelToken.Token);
                task = doAfter.WaitDoAfter(args);
                cancelToken.Cancel();
            });

            await server.WaitRunTicks(3);
            Assert.That(task.Result, Is.EqualTo(DoAfterStatus.Cancelled), $"Result was {task.Result}");
        }
    }
}
