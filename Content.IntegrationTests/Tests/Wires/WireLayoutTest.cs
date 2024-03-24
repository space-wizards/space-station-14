using Content.Server.Doors;
using Content.Server.Power;
using Content.Server.Wires;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Wires;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(WiresSystem))]
public sealed class WireLayoutTest
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

    [Test]
    public async Task TestLayoutInheritance()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var wires = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<WiresSystem>();

            // Need to spawn these entities to make sure the wire layouts are initialized.
            var ent1 = SpawnWithComp<WiresComponent>(server.EntMan, "WireLayoutTest", testMap.MapCoords);
            var ent2 = SpawnWithComp<WiresComponent>(server.EntMan, "WireLayoutTest2", testMap.MapCoords);
            var ent3 = SpawnWithComp<WiresComponent>(server.EntMan, "WireLayoutTest3", testMap.MapCoords);

            // Assert.That(wires.TryGetLayout("WireLayoutTest", out var layout1));
            // Assert.That(wires.TryGetLayout("WireLayoutTest2", out var layout2));
            // Assert.That(wires.TryGetLayout("WireLayoutTest3", out var layout3));

            Assert.Multiple(() =>
            {
                // Entity 1.
                Assert.That(ent1.Comp.WiresList, Has.Count.EqualTo(4));
                Assert.That(ent1.Comp.WiresList, Has.Exactly(2).With.Property("Action").Null, "2 dummy wires");
                Assert.That(ent1.Comp.WiresList, Has.One.With.Property("Action").InstanceOf<PowerWireAction>(), "1 power wire");
                Assert.That(ent1.Comp.WiresList, Has.One.With.Property("Action").InstanceOf<DoorBoltWireAction>(), "1 door bolt wire");

                Assert.That(ent2.Comp.WiresList, Has.Count.EqualTo(5));
                Assert.That(ent2.Comp.WiresList, Has.Exactly(2).With.Property("Action").Null, "2 dummy wires");
                Assert.That(ent2.Comp.WiresList, Has.Exactly(2).With.Property("Action").InstanceOf<PowerWireAction>(), "2 power wire");
                Assert.That(ent2.Comp.WiresList, Has.One.With.Property("Action").InstanceOf<DoorBoltWireAction>(), "1 door bolt wire");

                Assert.That(ent3.Comp.WiresList, Has.Count.EqualTo(4));
                Assert.That(ent3.Comp.WiresList, Has.Exactly(2).With.Property("Action").Null, "2 dummy wires");
                Assert.That(ent3.Comp.WiresList, Has.One.With.Property("Action").InstanceOf<PowerWireAction>(), "1 power wire");
                Assert.That(ent3.Comp.WiresList, Has.One.With.Property("Action").InstanceOf<DoorBoltWireAction>(), "1 door bolt wire");
            });
        });

        await pair.CleanReturnAsync();
    }

    private static Entity<T> SpawnWithComp<T>(IEntityManager entityManager, string prototype, MapCoordinates coords)
        where T : IComponent, new()
    {
        var ent = entityManager.Spawn(prototype, coords);
        var comp = entityManager.EnsureComponent<T>(ent);
        return new Entity<T>(ent, comp);
    }
}
