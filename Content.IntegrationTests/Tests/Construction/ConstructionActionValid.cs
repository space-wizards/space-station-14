using System.Text;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Construction.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction
{
    [TestFixture]
    public sealed class ConstructionActionValid : GameTest
    {
        [Dependency] private SharedEntityEffectsSystem _effects = default!;

        [Test]
        public async Task ConstructionGraphSpawnPrototypeValid()
        {
            var pair = Pair;
            var server = pair.Server;

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            var valid = true;
            var message = new StringBuilder();

            await server.WaitPost(() =>
            {
                foreach (var graph in protoMan.EnumeratePrototypes<ConstructionGraphPrototype>())
                {
                    foreach (var node in graph.Nodes.Values)
                    {
                        foreach (var effect in node.Actions)
                        {
                            if (_effects.TryApplyEffect(new EntityUid(), effect))
                                continue;

                            valid = false;
                            message.Append($"Invalid entity prototype \"{effect}\" on graph action in node \"{node.Name}\" of graph \"{graph.ID}\"\n");

                        }

                        foreach (var edge in node.Edges)
                        {
                            foreach (var effect in edge.Completed)
                            {
                                if (_effects.TryApplyEffect(new EntityUid(), effect))
                                    continue;

                                valid = false;
                                message.Append($"Invalid entity prototype \"{effect}\" on graph action in edge \"{edge.Target}\" of node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                            }
                        }
                    }
                }
            });

            Assert.That(valid, Is.True, $"One or more SpawnPrototype actions specified invalid entity prototypes!\n{message}");
        }

        [Test]
        public async Task ConstructionGraphEdgeValid()
        {
            var pair = Pair;
            var server = pair.Server;

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            var valid = true;
            var message = new StringBuilder();

            await server.WaitPost(() =>
            {
                foreach (var graph in protoMan.EnumeratePrototypes<ConstructionGraphPrototype>())
                {
                    foreach (var node in graph.Nodes.Values)
                    {
                        foreach (var edge in node.Edges)
                        {
                            if (graph.Nodes.ContainsKey(edge.Target))
                                continue;

                            valid = false;
                            message.Append(
                                $"Invalid target \"{edge.Target}\" in edge on node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                        }
                    }
                }
            });

            Assert.That(valid, Is.True, $"One or more edges specified invalid node targets!\n{message}");
        }
    }
}
