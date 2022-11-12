using System.Threading;
using System.Threading.Tasks;
using Content.Server.DoAfter;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.DoAfter
{
    [TestFixture]
    [TestOf(typeof(DoAfterComponent))]
    public sealed class DoAfterServerTest
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
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var doAfterSystem = entityManager.EntitySysManager.GetEntitySystem<DoAfterSystem>();

            // That it finishes successfully
            await server.WaitPost(() =>
            {
                var tickTime = 1.0f / IoCManager.Resolve<IGameTiming>().TickRate;
                var mob = entityManager.SpawnEntity("Dummy", MapCoordinates.Nullspace);
                var cancelToken = new CancellationTokenSource();
                var args = new DoAfterEventArgs(mob, tickTime / 2, cancelToken.Token);
                task = doAfterSystem.WaitDoAfter(args);
            });

            await server.WaitRunTicks(1);
            Assert.That(task.Status, Is.EqualTo(TaskStatus.RanToCompletion));
#pragma warning disable RA0004
            Assert.That(task.Result == DoAfterStatus.Finished);
#pragma warning restore RA0004

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestCancelled()
        {
            Task<DoAfterStatus> task = null;

            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var doAfterSystem = entityManager.EntitySysManager.GetEntitySystem<DoAfterSystem>();

            await server.WaitPost(() =>
            {
                var tickTime = 1.0f / IoCManager.Resolve<IGameTiming>().TickRate;

                var mob = entityManager.SpawnEntity("Dummy", MapCoordinates.Nullspace);
                var cancelToken = new CancellationTokenSource();
                var args = new DoAfterEventArgs(mob, tickTime * 2, cancelToken.Token);
                task = doAfterSystem.WaitDoAfter(args);
                cancelToken.Cancel();
            });

            await server.WaitRunTicks(3);
            Assert.That(task.Status, Is.EqualTo(TaskStatus.RanToCompletion));
#pragma warning disable RA0004
            Assert.That(task.Result, Is.EqualTo(DoAfterStatus.Cancelled), $"Result was {task.Result}");
#pragma warning restore RA0004

            await pairTracker.CleanReturnAsync();
        }
    }
}
