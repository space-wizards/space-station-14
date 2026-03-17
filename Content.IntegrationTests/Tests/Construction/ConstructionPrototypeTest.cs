using Content.IntegrationTests.Utility;
using Content.Server.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction
{
    [TestFixture]
    public sealed class ConstructionPrototypeTest
    {
        // discount linter for construction graphs
        // TODO: Create serialization validators for these?
        // Top test definitely can be but writing a serializer takes ages.

        private static string[] _constructablePrototypes = GameDataScrounger.EntitiesWithComponent("Construction");
        private static string[] _constructions = GameDataScrounger.PrototypesOfKind<ConstructionPrototype>();

        /// <summary>
        /// Checks every entity prototype with a construction component has a valid start node.
        /// </summary>
        [Test]
        [TestOf(typeof(ConstructionComponent))]
        [TestCaseSource(nameof(_constructablePrototypes))]
        [Description("Tests that a given entity specifies a valid node for construction, and optionally a valid one for deconstruction.")]
        public async Task ConstructionComponentValid(string protoKey)
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                var proto = protoMan.Index(protoKey);
                var construction = (ConstructionComponent)proto.Components["Construction"].Component;

                var graph = protoMan.Index<ConstructionGraphPrototype>(construction.Graph);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(graph.Nodes.ContainsKey(construction.Node),
                        $"Found no node \"{construction.Node}\" on graph \"{graph.ID}\" for entity \"{proto.ID}\"!");

                    if (construction.DeconstructionNode is not { } target)
                        return;

                    Assert.That(graph.Nodes.ContainsKey(target),
                        $"Invalid deconstruction node \"{target}\" on graph \"{graph.ID}\" for construction entity \"{proto.ID}\"!");
                }
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        [TestOf(typeof(ConstructionPrototype))]
        [TestCaseSource(nameof(_constructions))]
        [Description("Tests that a given construction prototype has a valid starting and target node, and a valid path between them.")]
        public async Task ConstructionFormsValidGraph(string protoKey)
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var protoMan = server.ResolveDependency<IPrototypeManager>();
            var entMan = server.ResolveDependency<IEntityManager>();

            await server.WaitAssertion(() =>
            {
                var proto = protoMan.Index<ConstructionPrototype>(protoKey);
                var start = proto.StartNode;
                var target = proto.TargetNode;
                var graph = protoMan.Index(proto.Graph);

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
                var nextId = next.Entity.GetId(null, null, new(entMan));
                Assert.That(nextId, Is.Not.Null,
                    $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) must specify an entity! Graph: {graph.ID}");
                Assert.That(protoMan.TryIndex(nextId, out EntityPrototype entity),
                    $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) specified an invalid entity prototype ({nextId} [{next.Entity}])");
                Assert.That(entity!.Components.ContainsKey("Construction"),
                    $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) specified an entity prototype ({next.Entity}) without a ConstructionComponent.");
#pragma warning restore NUnit2045
            });
            await pair.CleanReturnAsync();
        }
    }
}
