using System.Threading;
using System.Threading.Tasks;
using Content.Server.DoAfter;
using Content.Shared.DoAfter;
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

        public sealed class TestDoAfterSystem : EntitySystem
        {
            public override void Initialize()
            {
                SubscribeLocalEvent<DoAfterEvent<TestDoAfterData>>(OnTestDoAfterFinishEvent);
            }

            private void OnTestDoAfterFinishEvent(DoAfterEvent<TestDoAfterData> ev)
            {
                ev.AdditionalData.Cancelled = ev.Cancelled;
            }
        }

        private sealed class TestDoAfterData
        {
            public bool Cancelled;
        };

        [Test]
        public async Task TestFinished()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var doAfterSystem = entityManager.EntitySysManager.GetEntitySystem<DoAfterSystem>();
            var data = new TestDoAfterData();

            // That it finishes successfully
            await server.WaitPost(() =>
            {
                var tickTime = 1.0f / IoCManager.Resolve<IGameTiming>().TickRate;
                var mob = entityManager.SpawnEntity("Dummy", MapCoordinates.Nullspace);
                var cancelToken = new CancellationTokenSource();
                var args = new DoAfterEventArgs(mob, tickTime / 2, cancelToken.Token) { Broadcast = true };
                doAfterSystem.DoAfter(args, data);
            });

            await server.WaitRunTicks(1);
            Assert.That(data.Cancelled, Is.False);

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestCancelled()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var doAfterSystem = entityManager.EntitySysManager.GetEntitySystem<DoAfterSystem>();
            var data = new TestDoAfterData();

            await server.WaitPost(() =>
            {
                var tickTime = 1.0f / IoCManager.Resolve<IGameTiming>().TickRate;

                var mob = entityManager.SpawnEntity("Dummy", MapCoordinates.Nullspace);
                var cancelToken = new CancellationTokenSource();
                var args = new DoAfterEventArgs(mob, tickTime * 2, cancelToken.Token) { Broadcast = true };
                doAfterSystem.DoAfter(args, data);
                cancelToken.Cancel();
            });

            await server.WaitRunTicks(3);
            Assert.That(data.Cancelled, Is.False);

            await pairTracker.CleanReturnAsync();
        }
    }
}
