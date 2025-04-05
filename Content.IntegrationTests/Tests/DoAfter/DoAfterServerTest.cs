using System.Collections.Generic;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
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

            var entityManager = server.EntMan;
            var timing = server.ResolveDependency<IGameTiming>();
            var doAfterSystem = entityManager.System<SharedDoAfterSystem>();
            var ev = new TestDoAfterEvent();

            // That it finishes successfully
            await server.WaitPost(() =>
            {
                var mob = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                var args = new DoAfterArgs(entityManager, mob, timing.TickPeriod / 2, ev, null) { Broadcast = true };
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
            var entityManager = server.EntMan;
            var timing = server.ResolveDependency<IGameTiming>();
            var doAfterSystem = entityManager.System<SharedDoAfterSystem>();
            var ev = new TestDoAfterEvent();

            await server.WaitPost(() =>
            {
                var mob = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                var args = new DoAfterArgs(entityManager, mob, timing.TickPeriod * 2, ev, null) { Broadcast = true };

                Assert.That(doAfterSystem.TryStartDoAfter(args, out var id));

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
            var entityManager = server.EntMan;
            var timing = server.ResolveDependency<IGameTiming>();
            var doAfterSystem = entityManager.System<SharedDoAfterSystem>();
            var interactionSystem = entityManager.System<SharedInteractionSystem>();
            var ev = new TestDoAfterEvent();

            EntityUid mob = default;
            EntityUid target = default;
            EntityUid mob2 = default;
            EntityUid target2 = default;

            await server.WaitPost(() =>
            {
                mob = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                target = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                var args = new DoAfterArgs(entityManager, mob, timing.TickPeriod * 5, ev, null, target) { Broadcast = true };

                Assert.That(doAfterSystem.TryStartDoAfter(args));

                // Start a second do after with a different target
                mob2 = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                target2 = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                var args2 = new DoAfterArgs(entityManager, mob2, timing.TickPeriod * 5, ev, null, target2) { Broadcast = true };

                Assert.That(doAfterSystem.TryStartDoAfter(args2));
            });

            var list = new HashSet<EntityUid>();
            interactionSystem.GetEntitiesInteractingWithTarget(target, list);
            Assert.That(list, Is.EquivalentTo([mob]));

            interactionSystem.GetEntitiesInteractingWithTarget(target2, list);
            Assert.That(list, Is.EquivalentTo([mob2]));

            await pair.CleanReturnAsync();
        }
    }
}
