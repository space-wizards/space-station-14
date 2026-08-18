#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Doors;
using Content.Server.Power;
using Content.Server.Wires;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Wires;

[Parallelizable(ParallelScope.All)]
[TestOf(typeof(WiresSystem))]
public sealed class WireLayoutTest : GameTest
{
    [TestPrototypes]
    public const string Prototypes = """
        - type: wireLayout
          id: WireLayoutTest
          dummyWires: 2
          wires:
          - !type:PowerWireAction
          - !type:DoorBoltWireAction

        - type: wireLayout
          id: WireLayoutTest2
          parent: WireLayoutTest
          wires:
          - !type:PowerWireAction

        - type: wireLayout
          id: WireLayoutTest3
          parent: WireLayoutTest

        - type: entity
          id: WireLayoutTest
          components:
          - type: Wires
            layoutId: WireLayoutTest

        - type: entity
          id: WireLayoutTest2
          components:
          - type: Wires
            layoutId: WireLayoutTest2

        - type: entity
          id: WireLayoutTest3
          components:
          - type: Wires
            layoutId: WireLayoutTest3
        """;

    [SidedDependency(Side.Server)] private EntityQuery<WiresComponent> _sQuery;

    [Test]
    public async Task TestLayoutInheritance()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            // Need to spawn these entities to make sure the wire layouts are initialized.
            var ent1 = SSpawnAtPosition("WireLayoutTest", TestMap!.GridCoords);
            var ent2 = SSpawnAtPosition("WireLayoutTest2", TestMap!.GridCoords);
            var ent3 = SSpawnAtPosition("WireLayoutTest3", TestMap!.GridCoords);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sQuery.TryComp(ent1, out var wires1), Is.True);
                if (wires1 is not null)
                {
                    Assert.That(wires1.WiresList, Has.Count.EqualTo(4));
                    Assert.That(wires1.WiresList, Has.Exactly(2).With.Property("Action").Null, "Should have 2 dummy wires.");
                    Assert.That(wires1.WiresList, Has.One.With.Property("Action").InstanceOf<PowerWireAction>(), "Should have 1 power wire.");
                    Assert.That(wires1.WiresList, Has.One.With.Property("Action").InstanceOf<DoorBoltWireAction>(), "Should have 1 door bolt wire.");
                }

                Assert.That(_sQuery.TryComp(ent2, out var wires2), Is.True);
                if (wires2 is not null)
                {
                    Assert.That(wires2.WiresList, Has.Count.EqualTo(5));
                    Assert.That(wires2.WiresList, Has.Exactly(2).With.Property("Action").Null, "Should have 2 dummy wires.");
                    Assert.That(wires2.WiresList, Has.Exactly(2).With.Property("Action").InstanceOf<PowerWireAction>(), "Should have 2 power wires.");
                    Assert.That(wires2.WiresList, Has.One.With.Property("Action").InstanceOf<DoorBoltWireAction>(), "Should have 1 door bolt wire.");
                }

                Assert.That(_sQuery.TryComp(ent3, out var wires3), Is.True);
                if (wires3 is not null)
                {
                    Assert.That(wires3.WiresList, Has.Count.EqualTo(4));
                    Assert.That(wires3.WiresList, Has.Exactly(2).With.Property("Action").Null, "Should have 2 dummy wires.");
                    Assert.That(wires3.WiresList, Has.One.With.Property("Action").InstanceOf<PowerWireAction>(), "Should have 1 power wire.");
                    Assert.That(wires3.WiresList, Has.One.With.Property("Action").InstanceOf<DoorBoltWireAction>(), "Should have 1 door bolt wire.");
                }
            }
        });
    }
}
