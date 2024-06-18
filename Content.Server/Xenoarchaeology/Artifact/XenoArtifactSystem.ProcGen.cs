using Content.Shared.Random.Helpers;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Xenoarchaeology.Artifact;

public sealed partial class XenoArtifactSystem
{
    private void GenerateArtifactStructure(Entity<XenoArtifactComponent> ent)
    {
        var nodeCount = ent.Comp.NodeCount.Next(RobustRandom);
        ResizeNodeGraph(ent, nodeCount);
        while (nodeCount > 0)
        {
            GenerateArtifactSegment(ent, ref nodeCount);
        }

        RebuildCachedActiveNodes((ent, ent));
    }

    private void GenerateArtifactSegment(Entity<XenoArtifactComponent> ent, ref int nodeCount)
    {
        var segmentSize = GetArtifactSegmentSize(ent, nodeCount);
        nodeCount -= segmentSize;
        PopulateArtifactSegmentRecursive(ent, ref segmentSize);

        // TODO: store the segments in a list somewhere so we don't have to rebuild them constantly.
        // Or maybe just rebuild them manually like we do active nodes??? hard to say.
    }

    private List<Entity<XenoArtifactNodeComponent>> PopulateArtifactSegmentRecursive(Entity<XenoArtifactComponent> ent, ref int segmentSize, int layerMaxMod = 0)
    {
        if (segmentSize == 0)
            return new();

        var layerMin = ent.Comp.NodesPerSegmentLayer.Min;
        var layerMax = Math.Min(ent.Comp.NodesPerSegmentLayer.Max + layerMaxMod, segmentSize);

        // Default to one node if we had shenanigans and ended up with weird layer counts.
        var nodeCount = 1;
        if (layerMax >= layerMin)
            nodeCount = RobustRandom.Next(layerMin, layerMax + 1); // account for non-inclusive max

        segmentSize -= nodeCount;
        var nodes = new List<Entity<XenoArtifactNodeComponent>>();
        for (var i = 0; i < nodeCount; i++)
        {
            nodes.Add(CreateRandomNode(ent));
        }

        var layerMod = nodes.Count / 2; // cumulative modifier to enable slight growth for something like 3 -> 4
        var successors = PopulateArtifactSegmentRecursive(ent, ref segmentSize, layerMod);
        if (successors.Count == 0)
            return nodes;

        // We do the picks from node -> successor and from successor -> node to ensure that no nodes get orphaned without connections.
        foreach (var node in nodes)
        {
            var successor = RobustRandom.Pick(successors);
            AddEdge((ent, ent), node, successor);
        }
        foreach (var successor in successors)
        {
            var node = RobustRandom.Pick(nodes);
            AddEdge((ent, ent), node, successor);
        }

        // TODO: if gen is bad, consider implementing random scattering

        return nodes;
    }

    private int GetArtifactSegmentSize(Entity<XenoArtifactComponent> ent, int nodeCount)
    {
        // Make sure we can't generate a single segment artifact.
        // We always want to have at least 2 segments. For variety.
        var segmentMin = ent.Comp.SegmentSize.Min;
        var segmentMax = Math.Min(ent.Comp.SegmentSize.Max, Math.Max(nodeCount / 2, segmentMin));

        var segmentSize = RobustRandom.Next(segmentMin, segmentMax + 1); // account for non-inclusive max
        var remainder = nodeCount - segmentSize;

        // If our next segment is going to be undersized, then we just absorb it into this segment.
        if (remainder < ent.Comp.SegmentSize.Min)
            segmentSize += remainder;

        // Sanity check to make sure we don't exceed the node count. (it shouldn't happen prior anyway but oh well)
        segmentSize = Math.Min(nodeCount, segmentSize);

        return segmentSize;
    }

    private Entity<XenoArtifactNodeComponent> CreateRandomNode(Entity<XenoArtifactComponent> ent)
    {
        var proto = PrototypeManager.Index(ent.Comp.EffectWeights).Pick(RobustRandom);

        AddNode((ent, ent), proto, out var nodeEnt, dirty: false);
        DebugTools.Assert(nodeEnt.HasValue, "Failed to create node on artifact.");

        // TODO: setup trigger or effect or smth. idk quite how we're gonna do this.

        return nodeEnt.Value;
    }
}
