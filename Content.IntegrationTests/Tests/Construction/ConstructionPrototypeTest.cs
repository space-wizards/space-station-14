using System.Numerics;
using Content.Server.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction
{
    [TestFixture]
    public sealed class ConstructionPrototypeTest
    {
        // discount linter for construction graphs
        // TODO: Create serialization validators for these?
        // Top test definitely can be but writing a serializer takes ages.

        /// <summary>
        /// Checks every entity prototype with a construction component has a valid start node.
        /// </summary>
        [Test]
        public async Task TestStartNodeValid()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var entMan = server.ResolveDependency<IEntityManager>();
            var protoMan = server.ResolveDependency<IPrototypeManager>();

            var map = await pair.CreateTestMap();

            await server.WaitAssertion(() =>
            {
                foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.Components.ContainsKey("Construction"))
                        continue;

                    var ent = entMan.SpawnEntity(proto.ID, new MapCoordinates(Vector2.Zero, map.MapId));
                    var construction = entMan.GetComponent<ConstructionComponent>(ent);

                    var graph = protoMan.Index<ConstructionGraphPrototype>(construction.Graph);
                    entMan.DeleteEntity(ent);

                    Assert.That(graph.Nodes.ContainsKey(construction.Node),
                        $"Found no startNode \"{construction.Node}\" on graph \"{graph.ID}\" for entity \"{proto.ID}\"!");
                }
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestStartIsValid()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            foreach (var proto in protoMan.EnumeratePrototypes<ConstructionPrototype>())
            {
                var start = proto.StartNode;
                var graph = protoMan.Index<ConstructionGraphPrototype>(proto.Graph);

                Assert.That(graph.Nodes.ContainsKey(start), $"Found no startNode \"{start}\" on graph \"{graph.ID}\" for construction prototype \"{proto.ID}\"!");
            }
            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestTargetIsValid()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            foreach (var proto in protoMan.EnumeratePrototypes<ConstructionPrototype>())
            {
                var target = proto.TargetNode;
                var graph = protoMan.Index<ConstructionGraphPrototype>(proto.Graph);

                Assert.That(graph.Nodes.ContainsKey(target), $"Found no targetNode \"{target}\" on graph \"{graph.ID}\" for construction prototype \"{proto.ID}\"!");
            }
            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task DeconstructionIsValid()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var entMan = server.ResolveDependency<IEntityManager>();
            var protoMan = server.ResolveDependency<IPrototypeManager>();
            var compFact = server.ResolveDependency<IComponentFactory>();

            var name = compFact.GetComponentName(typeof(ConstructionComponent));
            Assert.Multiple(() =>
            {
                foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.Abstract || pair.IsTestPrototype(proto) || !proto.Components.TryGetValue(name, out var reg))
                        continue;

                    var comp = (ConstructionComponent) reg.Component;
                    var target = comp.DeconstructionNode;
                    if (target == null)
                        continue;

                    var graph = protoMan.Index<ConstructionGraphPrototype>(comp.Graph);
                    Assert.That(graph.Nodes.ContainsKey(target), $"Invalid deconstruction node \"{target}\" on graph \"{graph.ID}\" for construction entity \"{proto.ID}\"!");
                }
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestStartReachesValidTarget()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var protoMan = server.ResolveDependency<IPrototypeManager>();
            var entMan = server.ResolveDependency<IEntityManager>();

            foreach (var proto in protoMan.EnumeratePrototypes<ConstructionPrototype>())
            {
                var start = proto.StartNode;
                var target = proto.TargetNode;
                var graph = protoMan.Index<ConstructionGraphPrototype>(proto.Graph);

#pragma warning disable NUnit2045 // Interdependent assertions.
                Assert.That(graph.TryPath(start, target, out var path), $"Unable to find path from \"{start}\" to \"{target}\" on graph \"{graph.ID}\"");
                Assert.That(path, Has.Length.GreaterThanOrEqualTo(1), $"Unable to find path from \"{start}\" to \"{target}\" on graph \"{graph.ID}\".");
                var next = path[0];
                var nextId = next.Entity.GetId(null, null, new(entMan));
                Assert.That(nextId, Is.Not.Null, $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) must specify an entity! Graph: {graph.ID}");
                Assert.That(protoMan.TryIndex(nextId, out EntityPrototype entity), $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) specified an invalid entity prototype ({nextId} [{next.Entity}])");
                Assert.That(entity.Components.ContainsKey("Construction"), $"The next node ({next.Name}) in the path from the start node ({start}) to the target node ({target}) specified an entity prototype ({next.Entity}) without a ConstructionComponent.");
#pragma warning restore NUnit2045
            }
            await pair.CleanReturnAsync();
        }
    }
}
