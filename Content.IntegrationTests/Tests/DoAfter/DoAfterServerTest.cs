using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.DoAfter
{
    [TestFixture]
    [TestOf(typeof(DoAfterComponent))]
    public sealed partial class DoAfterServerTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: DoAfterDummy
  id: DoAfterDummy
  components:
  - type: DoAfter
";

        [Serializable, NetSerializable]
        private sealed partial class TestDoAfterEvent : DoAfterEvent
        {
            public override DoAfterEvent Clone()
            {
                return this;
            }
        };

        [Test]
        public async Task TestSerializable()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
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

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestFinished()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var timing = server.ResolveDependency<IGameTiming>();
            var doAfterSystem = entityManager.EntitySysManager.GetEntitySystem<SharedDoAfterSystem>();
            var ev = new TestDoAfterEvent();

            // That it finishes successfully
            await server.WaitPost(() =>
            {
                var tickTime = 1.0f / timing.TickRate;
                var mob = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                var args = new DoAfterArgs(entityManager, mob, tickTime / 2, ev, null) { Broadcast = true };
#pragma warning disable NUnit2045 // Interdependent assertions.
                Assert.That(doAfterSystem.TryStartDoAfter(args));
                Assert.That(ev.Cancelled, Is.False);
#pragma warning restore NUnit2045
            });

            await server.WaitRunTicks(1);
            Assert.That(ev.Cancelled, Is.False);

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestCancelled()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var timing = server.ResolveDependency<IGameTiming>();
            var doAfterSystem = entityManager.EntitySysManager.GetEntitySystem<SharedDoAfterSystem>();
            var ev = new TestDoAfterEvent();

            await server.WaitPost(() =>
            {
                var tickTime = 1.0f / timing.TickRate;

                var mob = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                var args = new DoAfterArgs(entityManager, mob, tickTime * 2, ev, null) { Broadcast = true };

                if (!doAfterSystem.TryStartDoAfter(args, out var id))
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

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestGetInteractingEntities()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var timing = server.ResolveDependency<IGameTiming>();
            var doAfterSystem = entityManager.EntitySysManager.GetEntitySystem<SharedDoAfterSystem>();
            var ev = new TestDoAfterEvent();

            EntityUid mob = default;
            EntityUid target = default;
            EntityUid mob2 = default;
            EntityUid target2 = default;

            await server.WaitPost(() =>
            {
                var tickTime = 1.0f / timing.TickRate;

                mob = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                target = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                var args = new DoAfterArgs(entityManager, mob, tickTime * 5, ev, null, target) { Broadcast = true };

                if (!doAfterSystem.TryStartDoAfter(args))
                {
                    Assert.Fail();
                    return;
                }

                // Start a second do after with a different target
                mob2 = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                target2 = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                var args2 = new DoAfterArgs(entityManager, mob2, tickTime * 5, ev, null, target2) { Broadcast = true };

                if (!doAfterSystem.TryStartDoAfter(args2))
                {
                    Assert.Fail();
                    return;
                }
            });

            // Run a single tick to trigger a DoAfterSystem update
            await server.WaitRunTicks(1);

            var list = doAfterSystem.GetEntitiesInteractingWithTarget(target);
            Assert.That(list, Has.Count.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(mob));

            var list2 = doAfterSystem.GetEntitiesInteractingWithTarget(target2);
            Assert.That(list2, Has.Count.EqualTo(1));
            Assert.That(list2[0], Is.EqualTo(mob2));

            await pair.CleanReturnAsync();
        }
    }
}
