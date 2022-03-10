using System.Linq;
using System.Threading.Tasks;
using Content.Server.Construction.Components;
using Content.Shared.Construction.Prototypes;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction
{
    [TestFixture]
    public sealed class ConstructionPrototypeTest : ContentIntegrationTest
    {
        // discount linter for construction graphs
        // TODO: Create serialization validators for these?

        [Test]
        public async Task TestStartIsValid()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            foreach (var proto in protoMan.EnumeratePrototypes<ConstructionPrototype>())
            {
                var start = proto.StartNode;
                var graph = protoMan.Index<ConstructionGraphPrototype>(proto.Graph);

                Assert.That(graph.Nodes.ContainsKey(start), $"Found no startNode \"{start}\" on graph \"{graph.ID}\" for construction prototype \"{proto.ID}\"!");
            }
        }

        [Test]
        public async Task TestTargetIsValid()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            foreach (var proto in protoMan.EnumeratePrototypes<ConstructionPrototype>())
            {
                var target = proto.TargetNode;
                var graph = protoMan.Index<ConstructionGraphPrototype>(proto.Graph);

                Assert.That(graph.Nodes.ContainsKey(target), $"Found no targetNode \"{target}\" on graph \"{graph.ID}\" for construction prototype \"{proto.ID}\"!");
            }
        }

        [Test]
        public async Task TestStartReachesValidTarget()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            foreach (var proto in protoMan.EnumeratePrototypes<ConstructionPrototype>())
            {
                var start = proto.StartNode;
                var target = proto.TargetNode;
                var graph = protoMan.Index<ConstructionGraphPrototype>(proto.Graph);
                Assert.That(graph.TryPath(start, target, out var path), $"Unable to find path from \"{start}\" to \"{target}\" on graph \"{graph.ID}\"");
                Assert.That(path!.Length, Is.GreaterThanOrEqualTo(1), $"Unable to find path from \"{start}\" to \"{target}\" on graph \"{graph.ID}\".");
                var next = path[0];
                Assert.That(next.Entity, Is.Not.Null, $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) must specify an entity! Graph: {graph.ID}");
                Assert.That(protoMan.TryIndex(next.Entity, out EntityPrototype entity), $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) specified an invalid entity prototype ({next.Entity})");
                Assert.That(entity.Components.ContainsKey("Construction"), $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) specified an entity prototype ({next.Entity}) without a ConstructionComponent.");
            }
        }
    }
}
