using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Construction.Components;
using Content.Shared.Temperature;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class EdgeClobbering : InteractionTest
{
    [TestPrototypes]
    private const string Prototypes = @"
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
  id: ExampleEntity
  components:
  - type: Construction
    graph: ExampleGraph
    node: A

    ";

    [Test]
    public async Task EnsureNoEdgeClobbering()
    {
        await SpawnTarget("ExampleEntity");
        var sTarget = SEntMan.GetEntity(Target!.Value);

        await InteractUsing(Screw, false);
        SEntMan.EventBus.RaiseLocalEvent(sTarget, new OnTemperatureChangeEvent(0f, 0f, 0f));
        await AwaitDoAfters();

        Assert.That(SEntMan.GetComponent<ConstructionComponent>(sTarget).Node, Is.EqualTo("C"));
    }
}
