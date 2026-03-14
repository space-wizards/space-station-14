using System.Collections.Generic;
using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Tiles;

public sealed class TileStackRecursionTest
{
    [Test]
    public async Task TestBaseTurfRecursion()
    {
        await using var pair = await PoolManager.GetServerClient();
        var protoMan = pair.Server.ResolveDependency<IPrototypeManager>();
        var cfg = pair.Server.ResolveDependency<IConfigurationManager>();
        var maxTileHistoryLength = cfg.GetCVar(CCVars.TileStackLimit);
        Assert.That(protoMan.TryGetInstances<ContentTileDefinition>(out var tiles));
        Assert.That(tiles, Is.Not.EqualTo(null));
        //store the distance from the root node to the given tile node
        var nodes = new List<(ProtoId<ContentTileDefinition>, int)>();
        //each element of list is a connection from BaseTurf tile to tile that goes on it
        var edges = new List<(ProtoId<ContentTileDefinition>, ProtoId<ContentTileDefinition>)>();
        foreach (var ctdef in tiles!.Values)
        {
            //at first, each node is unexplored and has infinite distance to root.
            //we use space node as root - everything is supposed to start at space, and it's hardcoded into the game anyway.
            if (ctdef.ID == ContentTileDefinition.SpaceID)
            {
                nodes.Insert(0, (ctdef.ID, 0)); //space is the first element
                continue;
            }
            Assert.That(ctdef.BaseTurf != ctdef.ID);
            nodes.Add((ctdef.ID, int.MaxValue));
            if (ctdef.BaseTurf != null)
                edges.Add((ctdef.BaseTurf.Value, ctdef.ID));
            Assert.That(ctdef.BaseWhitelist, Does.Not.Contain(ctdef.ID));
            edges.AddRange(ctdef.BaseWhitelist.Select(possibleTurf =>
                (possibleTurf, new ProtoId<ContentTileDefinition>(ctdef.ID))));
        }
        Bfs(nodes, edges, maxTileHistoryLength);
        await pair.CleanReturnAsync();
    }

    private void Bfs(List<(ProtoId<ContentTileDefinition>, int)> nodes, List<(ProtoId<ContentTileDefinition>, ProtoId<ContentTileDefinition>)> edges, int depthLimit)
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
            foreach (var node in adjNodes)
            {
                var adjNode = node;
                adjNode.Item2 = u.Item2 + 1;
                Assert.That(adjNode.Item2, Is.LessThanOrEqualTo(depthLimit)); //we can doomstack tiles on top of each other. Bad!
                queue.Enqueue(adjNode);
            }
        }
    }
}
