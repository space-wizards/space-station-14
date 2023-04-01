using System;
using System.Threading.Tasks;
using Content.Shared.DoAfter;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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

        private sealed class TestDoAfterEvent : DoAfterEvent
        {
            public override DoAfterEvent Clone()
            {
                return this;
            }
        };

        [Test]
        public async Task TestSerializable()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();
            var refMan = server.ResolveDependency<IReflectionManager>();

            await server.WaitPost(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var type in refMan.GetAllChildren<DoAfterEvent>(true))
                    {
                        if (type.IsAbstract || type == typeof(TestDoAfterEvent))
                            continue;

                        Assert.That(type.HasCustomAttribute<NetSerializableAttribute>()
                                    && type.HasCustomAttribute<SerializableAttribute>(),
                            $"{nameof(DoAfterEvent)} is not NetSerializable. Event: {type.Name}");
                    }
                });
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestFinished()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var doAfterSystem = entityManager.EntitySysManager.GetEntitySystem<SharedDoAfterSystem>();
            var ev = new TestDoAfterEvent();

            // That it finishes successfully
            await server.WaitPost(() =>
            {
                var tickTime = 1.0f / IoCManager.Resolve<IGameTiming>().TickRate;
                var mob = entityManager.SpawnEntity("Dummy", MapCoordinates.Nullspace);
                var args = new DoAfterArgs(mob, tickTime / 2, ev, null) { Broadcast = true };
                Assert.That(doAfterSystem.TryStartDoAfter(args));
                Assert.That(ev.Cancelled, Is.False);
            });

            await server.WaitRunTicks(1);
            Assert.That(ev.Cancelled, Is.False);

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestCancelled()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var doAfterSystem = entityManager.EntitySysManager.GetEntitySystem<SharedDoAfterSystem>();
            DoAfterId? id = default;
            var ev = new TestDoAfterEvent();


            await server.WaitPost(() =>
            {
                var tickTime = 1.0f / IoCManager.Resolve<IGameTiming>().TickRate;

                var mob = entityManager.SpawnEntity("Dummy", MapCoordinates.Nullspace);
                var args = new DoAfterArgs(mob, tickTime * 2, ev, null) { Broadcast = true };

                if (!doAfterSystem.TryStartDoAfter(args, out id))
                {
                    Assert.Fail();
                    return;
                }

                Assert.That(!ev.Cancelled);
                doAfterSystem.Cancel(id);
                Assert.That(ev.Cancelled);

            });

            await server.WaitRunTicks(3);
            Assert.That(ev.Cancelled);

            await pairTracker.CleanReturnAsync();
        }
    }
}
