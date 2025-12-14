using System.Collections.Generic;
using System.Linq;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Tiles;

public sealed class TileStacksTest
{
    //This is a magic value setting the hard limit on how much tiles can be stacked on top of each other.
    //Having it too high can result in "doomstacking" tiles - this messes with efficiency of explosions, deconstruction of tiles, and probably also results in memory problems.
    private const int MaxTileHistoryLength = 5;

    [Test]
    public async Task TestBaseTurfRecursion()
    {
        await using var pair = await PoolManager.GetServerClient();
        var protoMan = pair.Server.ResolveDependency<IPrototypeManager>();
        Assert.That(protoMan.TryGetInstances<ContentTileDefinition>(out var tiles));
        Assert.That(tiles, Is.Not.EqualTo(null));
        //bool? stands for the node exploration status, int stands for distance from root to this node
        var nodes = new List<(ProtoId<ContentTileDefinition>, int)>();
        //each element of list is a connection from BaseTurf tile to tile that goes on it
        var edges = new List<(ProtoId<ContentTileDefinition>, ProtoId<ContentTileDefinition>)>();
        foreach (var ctdef in tiles!.Values)
        {
            //at first, each node is unexplored and has infinite distance to root.
            //we use space node as root - everything is supposed to start at space, and it's hardcoded into the game anyway.
            if (ctdef.ID != ContentTileDefinition.SpaceID)
            {
                nodes.Add((ctdef.ID, int.MaxValue));
                edges.Add((ctdef.BaseTurf, ctdef.ID));
                if (ctdef.BaseWhitelist is null)
                    continue;
                edges.AddRange(ctdef.BaseWhitelist.Select(possibleTurf => (possibleTurf, new ProtoId<ContentTileDefinition>(ctdef.ID))));
            }
            else
            {
                nodes.Insert(0, (ctdef.ID, 0)); //space is the first element
            }
        }
        Bfs(nodes, edges);
        await pair.CleanReturnAsync();
    }

    private void Bfs(List<(ProtoId<ContentTileDefinition>, int)> nodes, List<(ProtoId<ContentTileDefinition>, ProtoId<ContentTileDefinition>)> edges)
    {
        var root = nodes[0];
        var queue = new Queue<(ProtoId<ContentTileDefinition>, int)>();
        queue.Enqueue(root);
        while (queue.Count != 0)
        {
            var u = queue.Dequeue();
            //get a list of tiles that can be put on this tile
            var adj = edges.Where(n => n.Item1 == u.Item1).Select(n => n.Item2);
            var adjNodes = nodes.Where(n => adj.Contains(n.Item1)).ToList();
            for (var i = 0; i < adjNodes.Count; i++)
            {
                var adjNode = adjNodes[i];
                adjNode.Item2 = u.Item2 + 1;
                Assert.That(adjNode.Item2, Is.LessThanOrEqualTo(MaxTileHistoryLength)); //we can doomstack tiles on top of each other. Bad!
                queue.Enqueue(adjNode);
            }
        }
    }
}
