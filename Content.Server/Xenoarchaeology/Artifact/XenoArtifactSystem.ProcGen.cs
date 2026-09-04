using System.Linq;
using Content.Shared.Whitelist;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Prototypes;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Xenoarchaeology.Artifact;

public sealed partial class XenoArtifactSystem
{
    [Dependency] private EntityTableSystem _entityTable = default!;

    /// <summary>
    /// Trigger for fallback scenario, when artifact acquired no trigger when generating artifact.
    /// </summary>
    private static readonly EntProtoId DummyTrigger = "TriggerExamine";

    private void GenerateArtifactStructure(Entity<XenoArtifactComponent> ent)
    {
        var nodeCount = ent.Comp.NodeCount.Next(RobustRandom);

        // trigger pool could be smaller, then requested node count
        var totalTriggers = _entityTable.ListSpawns(ent.Comp.TriggersTable)
                                        .Count();
        nodeCount = int.Min(nodeCount, totalTriggers);
        var triggerPoolData = new TriggerPoolData(nodeCount);

        ResizeNodeGraph(ent, nodeCount);
        while (nodeCount > 0)
        {
            GenerateArtifactSegment(ent, triggerPoolData, ref nodeCount);
        }

            desiredNodeCount -= generatedInSegment;
            totalGenerated += generatedInSegment;

    /// <summary>
    /// Generates segment of artifact - isolated graph, nodes inside which are interconnected.
    /// As size of segment is randomized - it is subtracted from node count.
    /// </summary>
    private int GenerateArtifactSegment(
        Entity<XenoArtifactComponent> ent,
        TriggerPoolData triggerPoolData,
        ref int nodeCount
    )
    {
        var segmentSize = GetArtifactSegmentSize(ent, nodeCount);
        nodeCount -= segmentSize;
        var populatedNodes = PopulateArtifactSegmentRecursive(ent, triggerPoolData, ref segmentSize);

        var segments = GetSegmentsFromNodes(ent, populatedNodes).ToList();

        // We didn't connect all of our nodes: do extra work to make sure there's a connection.
        if (segments.Count > 1)
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
        TriggerPoolData triggerPoolData,
        ref int segmentSize,
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
        var nodeCount = 1;
        if (layerMax >= layerMin)
            nodeCount = RobustRandom.Next((int)layerMin, (int)layerMax + 1); // account for non-inclusive max

        var nodes = new List<Entity<XenoArtifactNodeComponent>>();
        for (var i = 0; i < nodeCount; i++)
        {
            var trigger = _entityTable.GetFirstOrDefault(ent.Comp.TriggersTable, ctx: triggerPoolData.Context);
            if (trigger == null)
            {
                trigger = DummyTrigger;
                Log.Error(
                    "Failed to generate proper artifact - selector {selector} with excepted entities {excepted} "
                    + "provided zero triggers upon requesting new one",
                    ent.Comp.TriggersTable,
                    string.Join(", ", triggerPoolData.UsedTriggers.Select(x => x.Id))
                );
            }

            triggerPoolData.AddTriggerAsUsed(trigger.Value);
            nodes.Add(CreateNode(ent, trigger.Value, iteration));
        }

        var successors = PopulateArtifactSegmentRecursive(
            ent,
            triggerPoolData,
            ref segmentSize,
            iteration: iteration + 1
        );

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

        var segmentSize = RobustRandom.Next((int)segmentMin, (int)segmentMax + 1); // account for non-inclusive max
        var remainder = nodeCount - segmentSize;

        // If our next segment is going to be undersized, then we just absorb it into this segment.
        if (remainder < ent.Comp.SegmentSize.Min)
            segmentSize += remainder;

        // Sanity check to make sure we don't exceed the node count. (it shouldn't happen prior anyway but oh well)
        segmentSize = Math.Min(nodeCount, segmentSize);

        return segmentSize;
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
