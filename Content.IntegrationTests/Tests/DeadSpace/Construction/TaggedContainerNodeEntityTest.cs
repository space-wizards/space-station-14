// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

#nullable enable

using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.DeadSpace.Construction;

[TestFixture]
public sealed class TaggedContainerNodeEntityTest
{
    private static readonly ProtoId<ConstructionGraphPrototype> AirlockShuttleGraph = "AirlockShuttle";

    [Test]
    public async Task ShuttleAirlockUsesTaipanBoardTag()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entityManager = server.EntMan;
        var containerSystem = server.System<SharedContainerSystem>();
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var graph = prototypeManager.Index(AirlockShuttleGraph);

            Assert.Multiple(() =>
            {
                Assert.That(GetNodeEntity(graph, "airlock", null, entityManager), Is.EqualTo("AirlockShuttle"));
                Assert.That(GetNodeEntity(graph, "airlockGlass", null, entityManager), Is.EqualTo("AirlockGlassShuttle"));
            });

            var regularAssembly = CreateAssemblyWithBoard(entityManager, containerSystem, "DoorElectronics");
            var taipanAssembly = CreateAssemblyWithBoard(entityManager, containerSystem, "DoorElectronicsTaipan");

            Assert.Multiple(() =>
            {
                Assert.That(GetNodeEntity(graph, "airlock", regularAssembly, entityManager), Is.EqualTo("AirlockShuttle"));
                Assert.That(GetNodeEntity(graph, "airlockGlass", regularAssembly, entityManager), Is.EqualTo("AirlockGlassShuttle"));
                Assert.That(GetNodeEntity(graph, "airlock", taipanAssembly, entityManager), Is.EqualTo("AirlockShuttleSyndicate"));
                Assert.That(GetNodeEntity(graph, "airlockGlass", taipanAssembly, entityManager), Is.EqualTo("AirlockGlassShuttleSyndicate"));
            });
        });

        await pair.CleanReturnAsync();
    }

    private static EntityUid CreateAssemblyWithBoard(
        IEntityManager entityManager,
        SharedContainerSystem containerSystem,
        EntProtoId boardPrototype)
    {
        var assembly = entityManager.SpawnEntity(null, MapCoordinates.Nullspace);
        var board = entityManager.SpawnEntity(boardPrototype, MapCoordinates.Nullspace);
        var container = containerSystem.EnsureContainer<Container>(assembly, "board");

        Assert.That(containerSystem.Insert(board, container), Is.True);
        return assembly;
    }

    private static string? GetNodeEntity(
        ConstructionGraphPrototype graph,
        string node,
        EntityUid? assembly,
        IEntityManager entityManager)
    {
        return graph.Nodes[node].Entity.GetId(assembly, null, new GraphNodeEntityArgs(entityManager));
    }
}
