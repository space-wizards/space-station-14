#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Construction.Completions;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction;

public sealed class ConstructionActionValid : GameTest
{
    private static bool IsValid(IGraphAction action, IPrototypeManager protoMan, out string prototype)
    {
        switch (action)
        {
            case SpawnPrototype spawn:
                prototype = spawn.Prototype;
                return protoMan.TryIndex<EntityPrototype>(spawn.Prototype, out _);
            case SpawnPrototypeAtContainer spawn:
                prototype = spawn.Prototype;
                return protoMan.TryIndex<EntityPrototype>(spawn.Prototype, out _);
            case ConditionalAction conditional:
                var valid = IsValid(conditional.Action!, protoMan, out var protoA) & IsValid(conditional.Else!, protoMan, out var protoB);

                if (!string.IsNullOrEmpty(protoA) && string.IsNullOrEmpty(protoB))
                {
                    prototype = protoA;
                }

                else if (string.IsNullOrEmpty(protoA) && !string.IsNullOrEmpty(protoB))
                {
                    prototype = protoB;
                }

                else
                {
                    prototype = $"{protoA}, {protoB}";
                }

                return valid;
            default:
                prototype = string.Empty;
                return true;
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public async Task ConstructionGraphSpawnPrototypeValid()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var graph in SProtoMan.EnumeratePrototypes<ConstructionGraphPrototype>())
            {
                foreach (var node in graph.Nodes.Values)
                {
                    foreach (var action in node.Actions)
                    {
                        Assert.That(IsValid(action, SProtoMan, out var prototype),
                            $"Invalid entity prototype \"{prototype}\" on graph action in node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                    }

                    foreach (var edge in node.Edges)
                    {
                        foreach (var action in edge.Completed)
                        {
                            Assert.That(IsValid(action, SProtoMan, out var prototype),
                                $"Invalid entity prototype \"{prototype}\" on graph action in edge \"{edge.Target}\" of node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                        }
                    }
                }
            }
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public async Task ConstructionGraphEdgeValid()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var graph in SProtoMan.EnumeratePrototypes<ConstructionGraphPrototype>())
            {
                foreach (var node in graph.Nodes.Values)
                {
                    foreach (var edge in node.Edges)
                    {
                        Assert.That(graph.Nodes.ContainsKey(edge.Target),
                            $"Invalid target \"{edge.Target}\" in edge on node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                    }
                }
            }
        }
    }
}
