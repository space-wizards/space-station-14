using System.Linq;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Prototypes;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact;

public sealed partial class XenoArtifactSystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;

    private void GenerateArtifactStructure(Entity<XenoArtifactComponent> ent)
    {
        var desiredNodeCount = ent.Comp.NodeCount.Next(RobustRandom);
        var triggers = GetTriggers(ent);
        var effects = GetEffects(ent);
        var totalGenerated = 0;
        while (desiredNodeCount > 0)
        {
            var generatedInSegment = GenerateArtifactSegment(ent, triggers, effects, desiredNodeCount);

            desiredNodeCount -= generatedInSegment;
            totalGenerated += generatedInSegment;

            if (generatedInSegment == 0)
                break;
        }

        // trigger pool could be smaller, then requested node count
        ResizeNodeGraph(ent, totalGenerated);

        RebuildXenoArtifactMetaData((ent, ent));
    }

    /// <summary>
    /// Generates segment of artifact - isolated graph, nodes inside which are interconnected.
    /// As size of segment is randomized - it is subtracted from node count.
    /// </summary>
    private int GenerateArtifactSegment(
        Entity<XenoArtifactComponent> ent,
        Dictionary<XenoArchTriggerPrototype, float> triggers,
        Dictionary<EntityPrototype, float> effects,
        int maxNodeCount
    )
    {
        var nodesForSegmentToGenerate = GetArtifactSegmentDesiredSize(ent, maxNodeCount);
        var depth = 0;
        IReadOnlyCollection<Entity<XenoArtifactNodeComponent>> generatedNodes = [];
        List<Entity<XenoArtifactNodeComponent>> totalGenerated = new();
        while (nodesForSegmentToGenerate != 0)
        {
            generatedNodes = PopulateLayer(ent, triggers, effects, generatedNodes, nodesForSegmentToGenerate, depth);
            if(generatedNodes.Count == 0)
                break;

            nodesForSegmentToGenerate -= generatedNodes.Count;

            totalGenerated.AddRange(generatedNodes);
            depth++;
        }

        if (totalGenerated.Count == 0)
            return 0;

        AddEdgesToUnderConnectedNodes(ent, totalGenerated);

        return totalGenerated.Count;
    }

    /// <summary>
    /// Recursively populate layers of artifact segment - isolated graph, nodes inside which are interconnected.
    /// Each next iteration is going to have more chances to have more nodes (so it goes 'from top to bottom' of
    /// the tree, creating its peak nodes first, and then making layers with more and more branches).
    /// </summary>
    private IReadOnlyCollection<Entity<XenoArtifactNodeComponent>> PopulateLayer(
        Entity<XenoArtifactComponent> ent,
        Dictionary<XenoArchTriggerPrototype, float> triggers,
        Dictionary<EntityPrototype, float> effects,
        IReadOnlyCollection<Entity<XenoArtifactNodeComponent>> predecessors,
        int maxNodes,
        int iteration = 0
    )
    {
        if (maxNodes == 0)
            return [];

        // Try and get larger as we create more layers. Prevents excessive layers.
        var mod = RobustRandom.Next((int)(iteration / 1.5f), iteration + 1);

        var minPerLayer = Math.Min(ent.Comp.NodesPerSegmentLayer.Min + mod, maxNodes);
        var maxPerLayer = Math.Min(ent.Comp.NodesPerSegmentLayer.Max + mod, maxNodes);

        // Default to one node if we had shenanigans and ended up with weird layer counts.
        var desiredNodeCount = 1;
        if (maxPerLayer >= minPerLayer)
            desiredNodeCount = RobustRandom.Next(minPerLayer, maxPerLayer + 1); // account for non-inclusive max

        var nodes = new List<Entity<XenoArtifactNodeComponent>>();
        var scatterCount = ent.Comp.ScatterPerLayer.Next(RobustRandom);

        for (var i = 0; i < desiredNodeCount; i++)
        {
            var directPredecessors = SelectDirectPredecessors(predecessors, scatterCount);
            scatterCount-=(directPredecessors.Count - 1);

            var nodeEntity = CreateNode(ent, directPredecessors, triggers, effects, iteration);
            if (!nodeEntity.HasValue)
                continue;

            nodes.Add(nodeEntity.Value);

            foreach (var predecessorForEdge in directPredecessors)
            {
                AddEdge((ent, ent), predecessorForEdge, nodeEntity.Value, dirty: false);
            }
        }


        return nodes;
    }

    private List<Entity<XenoArtifactNodeComponent>> SelectDirectPredecessors(
        IReadOnlyCollection<Entity<XenoArtifactNodeComponent>> predecessors,
        int scatterCount
    )
    {
        List<Entity<XenoArtifactNodeComponent>> directPredecessors = new();
        ValueList<Entity<XenoArtifactNodeComponent>> predecessorsToUse = new(predecessors);
        if (predecessors.Count <= 0)
            return directPredecessors;

        var predecessor = RobustRandom.Pick(predecessorsToUse);
        directPredecessors.Add(predecessor);
        predecessorsToUse.Remove(predecessor);

        // randomly add in some extra edges for variance.
        while (scatterCount > 0 && predecessorsToUse.Count != 0)
        {
            scatterCount--;
            var predecessorFromScatter = RobustRandom.Pick(predecessorsToUse);
            directPredecessors.Add(predecessorFromScatter);
            predecessorsToUse.Remove(predecessor);
            if (RobustRandom.Prob(0.5f))
                break;
        }

        return directPredecessors;
    }

    /// <summary>
    /// Rolls segment size, based on amount of nodes left and XenoArtifactComponent settings.
    /// </summary>
    private int GetArtifactSegmentDesiredSize(Entity<XenoArtifactComponent> ent, int nodeCount)
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

    private Dictionary<XenoArchTriggerPrototype, float> GetTriggers(Entity<XenoArtifactComponent> ent)
    {
        var weightsProto = PrototypeManager.Index(ent.Comp.TriggerWeights);
        var weightByProto = new Dictionary<XenoArchTriggerPrototype, float>();
        foreach (var (triggerId, weight) in weightsProto.Weights)
        {
            var trigger = PrototypeManager.Index<XenoArchTriggerPrototype>(triggerId);
            if (_entityWhitelist.IsWhitelistFail(trigger.Whitelist, ent))
                continue;

            weightByProto.Add(trigger, weight);
        }
        return weightByProto;
    }

    private Dictionary<EntityPrototype, float> GetEffects(Entity<XenoArtifactComponent> ent)
    {
        var weightsProto = PrototypeManager.Index(ent.Comp.EffectsWeights);
        var weightByProto = new Dictionary<EntityPrototype, float>();
        foreach (var (effectProtoId, weight) in weightsProto.Weights)
        {
            var effect = PrototypeManager.Index<EntityPrototype>(effectProtoId);

            weightByProto.Add(effect, weight);
        }
        return weightByProto;
    }

    private void AddEdgesToUnderConnectedNodes(Entity<XenoArtifactComponent> ent, IReadOnlyCollection<Entity<XenoArtifactNodeComponent>> generated)
    {
        var segments = GetSegmentsFromNodes(ent, generated);

        // We didn't connect all of our nodes: do extra work to make sure there's a connection.
        if (segments.Count <= 1)
            return;

        var parent = segments.MaxBy(s => s.Count)!;
        var minP = parent.Min(n => n.Comp.Depth);
        var maxP = parent.Max(n => n.Comp.Depth);

        segments.Remove(parent);
        foreach (var segment in segments)
        {
            // calculate the range of the depth of the nodes in the segment
            var minS = segment.Min(n => n.Comp.Depth);
            var maxS = segment.Max(n => n.Comp.Depth);

            // Figure out the range of depths that allows for a connection between these two.
            // The range is essentially the lower values + 1 on each side.
            var min = Math.Max(minS, minP) - 1;
            var max = Math.Min(maxS, maxP) + 1;

            // how the fuck did you do this? you don't even deserve to get a parent. fuck you.
            if (min > max || min == max)
                continue;

            var node1Options = segment.Where(n => n.Comp.Depth >= min && n.Comp.Depth <= max)
                                      .ToList();
            if (node1Options.Count == 0)
                continue;

            var node1 = RobustRandom.Pick(node1Options);
            var node1Depth = node1.Comp.Depth;

            var node2Options = parent.Where(n => n.Comp.Depth >= node1Depth - 1 && n.Comp.Depth <= node1Depth + 1 && n.Comp.Depth != node1Depth)
                                     .ToList();
            if (node2Options.Count == 0)
                continue;

            var node2 = RobustRandom.Pick(node2Options);

            if (node1.Comp.Depth < node2.Comp.Depth)
                AddEdge((ent, ent.Comp), node1, node2, false);
            else
                AddEdge((ent, ent.Comp), node2, node1, false);
        }
    }
}
