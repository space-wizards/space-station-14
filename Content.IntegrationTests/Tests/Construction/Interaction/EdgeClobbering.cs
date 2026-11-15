#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Construction.Components;
using Content.Shared.Temperature;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class EdgeClobbering : InteractionTest
{
    private const string ExampleEntity = "ExampleEntity";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: constructionGraph
  id: ExampleGraph
  start: A
  graph:
  - node: A
    edges:
    - to: B
      steps:
      - tool: Anchoring
        doAfter: 1
    - to: C
      steps:
      - tool: Screwing
        doAfter: 1
  - node: B
  - node: C

- type: entity
  id: {ExampleEntity}
  components:
  - type: Construction
    graph: ExampleGraph
    node: A

    ";

    [Test]
    public async Task EnsureNoEdgeClobbering()
    {
        await SpawnTarget(ExampleEntity);

        await InteractUsing(Screw, false);
        var ev = new TemperatureChangedEvent(0f, 0f);
        SEntMan.EventBus.RaiseLocalEvent(STarget.Value, ref ev);
        await AwaitDoAfters();

        Assert.That(SComp<ConstructionComponent>(STarget.Value).Node, Is.EqualTo("C"));
    }
}
