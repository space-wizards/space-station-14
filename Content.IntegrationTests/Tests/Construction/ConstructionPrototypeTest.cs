#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.Construction.Components;
using Content.Shared.Construction.Prototypes;

namespace Content.IntegrationTests.Tests.Construction;

[TestFixture]
public sealed class ConstructionPrototypeTest : GameTest
{
    // discount linter for construction graphs
    // TODO: Create serialization validators for these?
    // Top test definitely can be but writing a serializer takes ages.

    private static readonly string[] ConstructablePrototypes = GameDataScrounger.EntitiesWithComponent("Construction");
    private static readonly string[] Constructions = GameDataScrounger.PrototypesOfKind<ConstructionPrototype>();

    /// <summary>
    /// Checks every entity prototype with a construction component has a valid start node.
    /// </summary>
    [Test]
    [TestOf(typeof(ConstructionComponent))]
    [TestCaseSource(nameof(ConstructablePrototypes))]
    [Description("Tests that a given entity specifies a valid node for construction, and optionally a valid one for deconstruction.")]
    [RunOnSide(Side.Server)]
    public async Task ConstructionComponentValid(string protoKey)
    {
        var proto = SProtoMan.Index(protoKey);
        proto.TryGetComponent<ConstructionComponent>(out var construction, SEntMan.ComponentFactory);
        Assert.That(construction, Is.Not.Null);

        var graph = SProtoMan.Index<ConstructionGraphPrototype>(construction.Graph);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(graph.Nodes.ContainsKey(construction.Node),
                $"Found no node \"{construction.Node}\" on graph \"{graph.ID}\" for entity \"{proto.ID}\"!");

            if (construction.DeconstructionNode is not { } target)
                return;

            Assert.That(graph.Nodes.ContainsKey(target),
                $"Invalid deconstruction node \"{target}\" on graph \"{graph.ID}\" for construction entity \"{proto.ID}\"!");
        }
    }

    [Test]
    [TestOf(typeof(ConstructionPrototype))]
    [TestCaseSource(nameof(Constructions))]
    [Description("Tests that a given construction prototype has a valid starting and target node, and a valid path between them.")]
    [RunOnSide(Side.Server)]
    public async Task ConstructionFormsValidGraph(string protoKey)
    {
        var proto = SProtoMan.Index<ConstructionPrototype>(protoKey);
        var start = proto.StartNode;
        var target = proto.TargetNode;
        var graph = SProtoMan.Index(proto.Graph);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(graph.Nodes.ContainsKey(start),
                $"Found no startNode \"{start}\" on graph \"{graph.ID}\"!");
            Assert.That(graph.Nodes.ContainsKey(target),
                $"Found no targetNode \"{target}\" on graph \"{graph.ID}\"!");
        }

#pragma warning disable NUnit2045 // Interdependent assertions.
        Assert.That(graph.TryPath(start, target, out var path),
            $"Unable to find path from \"{start}\" to \"{target}\" on graph \"{graph.ID}\"");
        Assert.That(path, Has.Length.GreaterThanOrEqualTo(1),
            $"Unable to find path from \"{start}\" to \"{target}\" on graph \"{graph.ID}\".");
        var next = path![0];
        var nextId = next.Entity.GetId(null, null, new(SEntMan));
        Assert.That(nextId, Is.Not.Null,
            $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) must specify an entity! Graph: {graph.ID}");
        Assert.That(SProtoMan.TryIndex(nextId, out var entity),
            $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) specified an invalid entity prototype ({nextId} [{next.Entity}])");
        Assert.That(entity!.Components.ContainsKey("Construction"),
            $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) specified an entity prototype ({next.Entity}) without a ConstructionComponent.");
#pragma warning restore NUnit2045
    }
}
