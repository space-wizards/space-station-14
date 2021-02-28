using System.Text;
using System.Threading.Tasks;
using Content.Server.Construction.Completions;
using Content.Shared.Construction;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction
{
    [TestFixture]
    public class ConstructionActionValid : ContentIntegrationTest
    {
        [Test]
        public async Task ConstructionGraphSpawnPrototypeValid()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            var valid = true;
            var message = new StringBuilder();

            foreach (var graph in protoMan.EnumeratePrototypes<ConstructionGraphPrototype>())
            {
                foreach (var (_, node) in graph.Nodes)
                {
                    foreach (var action in node.Actions)
                    {
                        if (action is not SpawnPrototype spawn || protoMan.TryIndex(spawn.Prototype, out EntityPrototype _)) continue;

                        valid = false;
                        message.Append($"Invalid entity prototype \"{spawn.Prototype}\" on graph action in node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                    }

                    foreach (var edge in node.Edges)
                    {
                        foreach (var action in edge.Completed)
                        {
                            if (action is not SpawnPrototype spawn || protoMan.TryIndex(spawn.Prototype, out EntityPrototype _)) continue;

                            valid = false;
                            message.Append($"Invalid entity prototype \"{spawn.Prototype}\" on graph action in edge \"{edge.Target}\" of node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                        }
                    }
                }
            }

            Assert.That(valid, Is.True, $"One or more SpawnPrototype actions specified invalid entity prototypes!\n{message}");
        }

        [Test]
        public async Task ConstructionGraphNodeEntityPrototypeValid()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            var valid = true;
            var message = new StringBuilder();

            foreach (var graph in protoMan.EnumeratePrototypes<ConstructionGraphPrototype>())
            {
                foreach (var (_, node) in graph.Nodes)
                {
                    if (string.IsNullOrEmpty(node.Entity) || protoMan.TryIndex(node.Entity, out EntityPrototype _)) continue;

                    valid = false;
                    message.Append($"Invalid entity prototype \"{node.Entity}\" on node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                }
            }

            Assert.That(valid, Is.True, $"One or more nodes specified invalid entity prototypes!\n{message}");
        }

        [Test]
        public async Task ConstructionGraphEdgeValid()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            var valid = true;
            var message = new StringBuilder();

            foreach (var graph in protoMan.EnumeratePrototypes<ConstructionGraphPrototype>())
            {
                foreach (var (_, node) in graph.Nodes)
                {
                    foreach (var edge in node.Edges)
                    {
                        if (graph.Nodes.ContainsKey(edge.Target)) continue;

                        valid = false;
                        message.Append($"Invalid target \"{edge.Target}\" in edge on node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                    }
                }
            }

            Assert.That(valid, Is.True, $"One or more edges specified invalid node targets!\n{message}");
        }
    }
}
