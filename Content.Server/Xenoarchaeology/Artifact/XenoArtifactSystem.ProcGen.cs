using System.Linq;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact;

public sealed partial class XenoArtifactSystem
{
    [Dependency] private EntityTableSystem _entityTable = default!;

    private void GenerateArtifactStructure(Entity<XenoArtifactComponent> ent)
    {
        var desiredNodeCount = ent.Comp.NodeCount.Next(RobustRandom);
        var triggers = ent.Comp.TriggersTable;
        var effects = ent.Comp.EffectsTable;
        var totalGenerated = 0;

        var triggerPool = new TriggerPoolData(desiredNodeCount);

        while (desiredNodeCount > 0)
        {
            var generatedInSegment = GenerateArtifactSegment(ent, triggers, effects, triggerPool, desiredNodeCount);

            desiredNodeCount -= generatedInSegment;
            totalGenerated += generatedInSegment;

            if (generatedInSegment == 0)
                break;
        }

        // trigger pool could be smaller, then requested node count
        ResizeNodeGraph(ent, totalGenerated);

        RebuildXenoArtifactMetaData((ent, ent));
    }

    private int GenerateArtifactSegment(
        Entity<XenoArtifactComponent> ent,
        EntityTableSelector triggers,
        EntityTableSelector effects,
        TriggerPoolData triggerPool,
        int maxNodeCount
    )
    {
        var nodesForSegmentToGenerate = GetArtifactSegmentDesiredSize(ent, maxNodeCount);
        var depth = 0;
        IReadOnlyCollection<Entity<XenoArtifactNodeComponent>> generatedNodes = [];
        List<Entity<XenoArtifactNodeComponent>> totalGenerated = new();
        while (nodesForSegmentToGenerate != 0)
        {
            generatedNodes = PopulateLayer(ent, triggers, effects, generatedNodes, triggerPool, nodesForSegmentToGenerate, depth);
            if (generatedNodes.Count == 0) // failed to generate nodes - time to finish up
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
        EntityTableSelector triggers,
        EntityTableSelector effects,
        IReadOnlyCollection<Entity<XenoArtifactNodeComponent>> predecessors,
        TriggerPoolData triggerPool,
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
            desiredNodeCount = new MinMax(minPerLayer, maxPerLayer).Next(RobustRandom);

        var nodes = new List<Entity<XenoArtifactNodeComponent>>();
        var scatterCount = ent.Comp.ScatterPerLayer.Next(RobustRandom);

        for (var i = 0; i < desiredNodeCount; i++)
        {
            var directPredecessors = SelectDirectPredecessors(predecessors, scatterCount);
            scatterCount -= (directPredecessors.Count - 1);
           
            var trigger = _entityTable.GetSpawns(triggers, RobustRandom, triggerPool.Context)
                                      .FirstOrDefault();
            // TODO: handle null

            var nodeEntity = CreateNode(ent, directPredecessors, trigger, effects, iteration);
            if (!nodeEntity.HasValue)
                continue;

            triggerPool.AddTriggerAsUsed(trigger);

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
        var segmentMax = Math.Min(ent.Comp.SegmentSize.Max, Math.Max((float)nodeCount / 2, segmentMin));

        var segmentSize = RobustRandom.Next((int)segmentMin, (int)segmentMax + 1); // account for non-inclusive max
        var remainder = nodeCount - segmentSize;

        // If our next segment is going to be undersized, then we just absorb it into this segment.
        if (remainder < ent.Comp.SegmentSize.Min)
            segmentSize += remainder;

        // Sanity check to make sure we don't exceed the node count. (it shouldn't happen prior anyway but oh well)
        segmentSize = Math.Min(nodeCount, segmentSize);

        return segmentSize;
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

    /// <summary>
    /// Container that represents pool of XenoArtifact triggers.
    /// </summary>
    private sealed class TriggerPoolData
    {
        private readonly HashSet<EntProtoId> _usedTriggers;

        public TriggerPoolData(int requestedSize)
        {
            _usedTriggers = new(requestedSize);
            Context = new EntityTableContext(new Dictionary<string, object>
            {
                [ExcludeEntitiesFromContextCondition.EntitiesToExclude] = _usedTriggers
            });
        }

        public readonly EntityTableContext Context;

        public void AddTriggerAsUsed(EntProtoId trigger)
        {
            if (!_usedTriggers.Add(trigger))
                throw new ArgumentException();
        }

        public IReadOnlyCollection<EntProtoId> UsedTriggers => _usedTriggers;
    }
}
