#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.DoAfter;

[TestOf(typeof(DoAfterComponent))]
public sealed partial class DoAfterServerTest : GameTest
{
    private const string DoAfterDummy = "DoAfterDummy";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {DoAfterDummy}
  id: {DoAfterDummy}
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

    [SidedDependency(Side.Server)] private IReflectionManager _sRefMan = null!;
    [SidedDependency(Side.Server)] private SharedDoAfterSystem _sDoAfterSystem = null!;
    [SidedDependency(Side.Server)] private SharedInteractionSystem _sInteractionSystem = null!;

    [Test]
    [RunOnSide(Side.Server)]
    [Description($"Tests that all non-abstract Types inheriting from {nameof(DoAfterEvent)} are NetSerializable.")]
    public async Task TestSerializable()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var type in _sRefMan.GetAllChildren<DoAfterEvent>(true))
            {
                if (type.IsAbstract || type == typeof(TestDoAfterEvent))
                    continue;

                Assert.That(type.HasCustomAttribute<NetSerializableAttribute>()
                            && type.HasCustomAttribute<SerializableAttribute>(),
                    $"{nameof(DoAfterEvent)} is not NetSerializable. Event: {type.Name}");
            }
        }
    }

    [Test]
    [Description("Tests that a DoAfter finishes successfully.")]
    public async Task TestFinished()
    {
        var ev = new TestDoAfterEvent();

        await Server.WaitPost(() =>
        {
            var mob = SSpawn(DoAfterDummy);
            var args = new DoAfterArgs(SEntMan, mob, SGameTiming.TickPeriod / 2, ev, null) { Broadcast = true };
#pragma warning disable NUnit2045 // Interdependent assertions.
            Assert.That(_sDoAfterSystem.TryStartDoAfter(args));
            Assert.That(ev.Cancelled, Is.False);
#pragma warning restore NUnit2045
        });

        await RunTicksSync(1);

        Assert.That(ev.Cancelled, Is.False);
    }

    [Test]
    [Description("Tests that a DoAfter can be cancelled.")]
    public async Task TestCancelled()
    {
        var ev = new TestDoAfterEvent();

        await Server.WaitPost(() =>
        {
            var mob = SSpawn(DoAfterDummy);
            var args = new DoAfterArgs(SEntMan, mob, SGameTiming.TickPeriod * 2, ev, null) { Broadcast = true };

            Assert.That(_sDoAfterSystem.TryStartDoAfter(args, out var id));
            Assert.That(!ev.Cancelled);

            _sDoAfterSystem.Cancel(id);
            Assert.That(ev.Cancelled);
        });

        await RunTicksSync(3);
        Assert.That(ev.Cancelled);
    }

    /// <summary>
    /// Spawns two sets of mobs with a targeted DoAfter to check that the GetEntitiesInteractingWithTarget result
    /// includes the correct interacting entities.
    /// </summary>
    [Test]
    [TestOf(typeof(SharedInteractionSystem))]
    [Description($"Tests that mobs performing targeted DoAfters are detected by {nameof(SharedInteractionSystem.GetEntitiesInteractingWithTarget)}.")]
    [RunOnSide(Side.Server)]
    public async Task TestGetInteractingEntities()
    {
        var ev = new TestDoAfterEvent();

        // Spawn two targets to interact with
        var target = SSpawn(DoAfterDummy);
        var target2 = SSpawn(DoAfterDummy);

        // Spawn a mob which is interacting with the first target
        var mob = SSpawn(DoAfterDummy);
        var args = new DoAfterArgs(SEntMan, mob, SGameTiming.TickPeriod * 5, ev, null, target) { Broadcast = true };
        Assert.That(_sDoAfterSystem.TryStartDoAfter(args));

        // Spawn two more mobs which are interacting with the second target
        var mob2 = SSpawn(DoAfterDummy);
        var args2 = new DoAfterArgs(SEntMan, mob2, SGameTiming.TickPeriod * 5, ev, null, target2) { Broadcast = true };
        Assert.That(_sDoAfterSystem.TryStartDoAfter(args2));

        var mob3 = SSpawn(DoAfterDummy);
        var args3 = new DoAfterArgs(SEntMan, mob3, SGameTiming.TickPeriod * 5, ev, null, target2) { Broadcast = true };
        Assert.That(_sDoAfterSystem.TryStartDoAfter(args3));

        var list = new HashSet<EntityUid>();
        _sInteractionSystem.GetEntitiesInteractingWithTarget(target, list);
        Assert.That(list, Is.EquivalentTo([mob]), $"{mob} was not considered to be interacting with {target}");

        _sInteractionSystem.GetEntitiesInteractingWithTarget(target2, list);
        Assert.That(list, Is.EquivalentTo([mob2, mob3]), $"{mob2} and {mob3} were not considered to be interacting with {target2}");
    }
}
