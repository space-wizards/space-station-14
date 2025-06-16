using System.Linq;
using Content.Shared.EntityTable;
using Content.Shared.NameIdentifier;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Xenoarchaeology.Artifact;

public abstract partial class SharedXenoArtifactSystem
{
    [Dependency] private readonly EntityTableSystem _entityTable =  default!;

    private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery;
    private EntityQuery<XenoArtifactNodeComponent> _nodeQuery;

    private void InitializeNode()
    {
        SubscribeLocalEvent<XenoArtifactNodeComponent, MapInitEvent>(OnNodeMapInit);

        _xenoArtifactQuery = GetEntityQuery<XenoArtifactComponent>();
        _nodeQuery = GetEntityQuery<XenoArtifactNodeComponent>();
    }

    /// <summary>
    /// Initializes artifact node on its creation (by setting durability).
    /// </summary>
    private void OnNodeMapInit(Entity<XenoArtifactNodeComponent> ent, ref MapInitEvent args)
    {
        XenoArtifactNodeComponent nodeComponent = ent;
        nodeComponent.MaxDurability -= nodeComponent.MaxDurabilityCanDecreaseBy.Next(RobustRandom);
        SetNodeDurability((ent, ent), nodeComponent.MaxDurability);
    }

    /// <summary> Gets node component by node entity uid. </summary>
    public XenoArtifactNodeComponent XenoArtifactNode(EntityUid uid)
    {
        return _nodeQuery.Get(uid);
    }

    public void SetNodeUnlocked(Entity<XenoArtifactNodeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Attached is not { } netArtifact)
            return;

        var artifact = GetEntity(netArtifact);
        if (!TryComp<XenoArtifactComponent>(artifact, out var artifactComponent))
            return;

        SetNodeUnlocked((artifact, artifactComponent), (ent, ent.Comp));
    }

    public void SetNodeUnlocked(Entity<XenoArtifactComponent> artifact, Entity<XenoArtifactNodeComponent> node)
    {
        if (!node.Comp.Locked)
            return;

        node.Comp.Locked = false;
        RebuildCachedActiveNodes((artifact, artifact));
        Dirty(node);
    }

    /// <summary>
    /// Adds to the node's durability by the specified value. To reduce, provide negative value.
    /// </summary>
    public void AdjustNodeDurability(Entity<XenoArtifactNodeComponent?> ent, int durabilityDelta)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        SetNodeDurability(ent, ent.Comp.Durability + durabilityDelta);
    }

    /// <summary>
    /// Sets a node's durability to the specified value. HIGHLY recommended to not be less than 0.
    /// </summary>
    public void SetNodeDurability(Entity<XenoArtifactNodeComponent?> ent, int durability)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Durability = Math.Clamp(durability, 0, ent.Comp.MaxDurability);
        UpdateNodeResearchValue((ent, ent.Comp));
        Dirty(ent);
    }

    /// <summary>
    /// Creates artifact node entity, attaching trigger and marking depth level for future use.
    /// </summary>
    public Entity<XenoArtifactNodeComponent> CreateNode(Entity<XenoArtifactComponent> ent, ProtoId<XenoArchTriggerPrototype> trigger, int depth = 0)
    {
        var triggerProto = PrototypeManager.Index(trigger);
        return CreateNode(ent, triggerProto, depth);
    }

    /// <summary>
    /// Creates artifact node entity, attaching trigger and marking depth level for future use.
    /// </summary>
    public Entity<XenoArtifactNodeComponent> CreateNode(Entity<XenoArtifactComponent> ent, XenoArchTriggerPrototype trigger, int depth = 0)
    {
        var entProtoId = _entityTable.GetSpawns(ent.Comp.EffectsTable)
                                     .First();

        AddNode((ent, ent), entProtoId, out var nodeEnt, dirty: false);
        DebugTools.Assert(nodeEnt.HasValue, "Failed to create node on artifact.");

        var nodeComponent = nodeEnt.Value.Comp;
        nodeComponent.Depth = depth;
        nodeComponent.TriggerTip = trigger.Tip;
        EntityManager.AddComponents(nodeEnt.Value, trigger.Components);

        Dirty(nodeEnt.Value);
        return nodeEnt.Value;
    }

    /// <summary> Checks if all predecessor nodes are marked as 'unlocked'. </summary>
    public bool HasUnlockedPredecessor(Entity<XenoArtifactComponent> ent, EntityUid node)
    {
        var predecessors = GetDirectPredecessorNodes((ent, ent), node);
        if (predecessors.Count == 0)
        {
            return true;
        }

        foreach (var predecessor in predecessors)
        {
            if (predecessor.Comp.Locked)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary> Checks if node was marked as 'active'. Active nodes are invoked on artifact use (if durability is greater than zero). </summary>
    public bool IsNodeActive(Entity<XenoArtifactComponent> ent, EntityUid node)
    {
        return ent.Comp.CachedActiveNodes.Contains(GetNetEntity(node));
    }

    /// <summary>
    /// Gets list of 'active' nodes. Active nodes are invoked on artifact use (if durability is greater than zero).
    /// </summary>
    public List<Entity<XenoArtifactNodeComponent>> GetActiveNodes(Entity<XenoArtifactComponent> ent)
    {
        return ent.Comp.CachedActiveNodes
                  .Select(activeNode => _nodeQuery.Get(GetEntity(activeNode)))
                  .ToList();
    }

    /// <summary>
    /// Gets amount of research points that can be extracted from node.
    /// We can only extract "what's left" - its base value, reduced by already consumed value.
    /// Every drained durability brings more points to be extracted.
    /// </summary>
    public int GetResearchValue(Entity<XenoArtifactNodeComponent> ent)
    {
        if (ent.Comp.Locked)
            return 0;

        return Math.Max(0, ent.Comp.ResearchValue - ent.Comp.ConsumedResearchValue);
    }

    /// <summary>
    /// Sets amount of points already extracted from node.
    /// </summary>
    public void SetConsumedResearchValue(Entity<XenoArtifactNodeComponent> ent, int value)
    {
        ent.Comp.ConsumedResearchValue = value;
        Dirty(ent);
    }

    /// <summary>
    /// Converts node entity uid to its display name (which is Identifier from <see cref="NameIdentifierComponent"/>.
    /// </summary>
    public string GetNodeId(EntityUid uid)
    {
        return (CompOrNull<NameIdentifierComponent>(uid)?.Identifier ?? 0).ToString("D3");
    }

    /// <summary>
    /// Gets two-dimensional array in a form of nested lists, which holds artifact nodes, grouped by segments.
    /// Segments are groups of interconnected nodes, there might be one or more segments in non-empty artifact.
    /// </summary>
    public List<List<Entity<XenoArtifactNodeComponent>>> GetSegments(Entity<XenoArtifactComponent> ent)
    {
        var output = new List<List<Entity<XenoArtifactNodeComponent>>>();

        foreach (var segment in ent.Comp.CachedSegments)
        {
            var outSegment = new List<Entity<XenoArtifactNodeComponent>>();
            foreach (var netNode in segment)
            {
                var node = GetEntity(netNode);
                outSegment.Add((node, XenoArtifactNode(node)));
            }

            output.Add(outSegment);
        }

        return output;
    }

    /// <summary>
    /// Gets list of nodes, grouped by depth level. Depth level count starts from 0.
    /// Only 0 depth nodes have no incoming edges - as only they are starting nodes.
    /// </summary>
    public Dictionary<int, List<Entity<XenoArtifactNodeComponent>>> GetDepthOrderedNodes(IEnumerable<Entity<XenoArtifactNodeComponent>> nodes)
    {
        var nodesByDepth = new Dictionary<int, List<Entity<XenoArtifactNodeComponent>>>();

        foreach (var node in nodes)
        {
            if (!nodesByDepth.TryGetValue(node.Comp.Depth, out var depthList))
            {
                depthList = new List<Entity<XenoArtifactNodeComponent>>();
                nodesByDepth.Add(node.Comp.Depth, depthList);
            }

            depthList.Add(node);
        }

        return nodesByDepth;
    }

    /// <summary>
    /// Rebuilds all the data, associated with nodes in an artifact, updating caches.
    /// </summary>
    public void RebuildXenoArtifactMetaData(Entity<XenoArtifactComponent?> artifact)
    {
        if (!Resolve(artifact, ref artifact.Comp))
            return;

        RebuildCachedActiveNodes(artifact);
        RebuildCachedSegments(artifact);
        foreach (var node in GetAllNodes((artifact, artifact.Comp)))
        {
            RebuildNodeMetaData(node);
        }

        CancelUnlockingOnGraphStructureChange((artifact, artifact.Comp));
    }

    public void RebuildNodeMetaData(Entity<XenoArtifactNodeComponent> node)
    {
        UpdateNodeResearchValue(node);
    }

    /// <summary>
    /// Clears all cached active nodes and rebuilds the list using the current node state.
    /// Active nodes have the following property:
    /// - Are unlocked themselves
    /// - All successors are also unlocked
    /// </summary>
    /// <remarks>
    /// You could technically modify this to have a per-node method that only checks direct predecessors
    /// and then does recursive updates for all successors, but I don't think the optimization is necessary right now.
    /// </remarks>
    public void RebuildCachedActiveNodes(Entity<XenoArtifactComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.CachedActiveNodes.Clear();
        var allNodes = GetAllNodes((ent, ent.Comp));
        foreach (var node in allNodes)
        {
            // Locked nodes cannot be active.
            if (node.Comp.Locked)
                continue;

            var successors = GetDirectSuccessorNodes(ent, node);

            // If this node has no successors, then we don't need to bother with this extra logic.
            if (successors.Count != 0)
            {
                // Checks for any of the direct successors being unlocked.
                var successorIsUnlocked = false;
                foreach (var sNode in successors)
                {
                    if (sNode.Comp.Locked)
                        continue;

                    successorIsUnlocked = true;
                    break;
                }

                // Active nodes must be at the end of the path.
                if (successorIsUnlocked)
                    continue;
            }

            var netEntity = GetNetEntity(node);
            ent.Comp.CachedActiveNodes.Add(netEntity);
        }

        Dirty(ent);
    }

    public void RebuildCachedSegments(Entity<XenoArtifactComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.CachedSegments.Clear();

        var entities = GetAllNodes((ent, ent.Comp))
            .ToList();
        var segments = GetSegmentsFromNodes((ent, ent.Comp), entities);
        var netEntities = segments.Select(
            s => s.Select(n => GetNetEntity(n))
                  .ToList()
        );
        ent.Comp.CachedSegments.AddRange(netEntities);

        Dirty(ent);
    }

    /// <summary>
    /// Gets two-dimensional array (as lists inside enumeration) that contains artifact nodes, grouped by segment.
    /// </summary>
    public IEnumerable<List<Entity<XenoArtifactNodeComponent>>> GetSegmentsFromNodes(Entity<XenoArtifactComponent> ent, List<Entity<XenoArtifactNodeComponent>> nodes)
    {
        var outSegments = new List<List<Entity<XenoArtifactNodeComponent>>>();
        foreach (var node in nodes)
        {
            var segment = new List<Entity<XenoArtifactNodeComponent>>();
            GetSegmentNodesRecursive(ent, node, segment, outSegments);

            if (segment.Count == 0)
                continue;

            outSegments.Add(segment);
        }

        return outSegments;
    }

    /// <summary>
    /// Fills nodes into segments by recursively walking through collections of predecessors and successors.
    /// </summary>
    private void GetSegmentNodesRecursive(
        Entity<XenoArtifactComponent> ent,
        Entity<XenoArtifactNodeComponent> node,
        List<Entity<XenoArtifactNodeComponent>> segment,
        List<List<Entity<XenoArtifactNodeComponent>>> otherSegments
    )
    {
        if (otherSegments.Any(s => s.Contains(node)))
            return;

        if (segment.Contains(node))
            return;

        segment.Add(node);

        var predecessors = GetDirectPredecessorNodes((ent, ent), node);
        foreach (var p in predecessors)
        {
            GetSegmentNodesRecursive(ent, p, segment, otherSegments);
        }

        var successors = GetDirectSuccessorNodes((ent, ent), node);
        foreach (var s in successors)
        {
            GetSegmentNodesRecursive(ent, s, segment, otherSegments);
        }
    }

    /// <summary>
    /// Sets node research point amount that can be extracted.
    /// Used up durability increases amount to be extracted.
    /// </summary>
    public void UpdateNodeResearchValue(Entity<XenoArtifactNodeComponent> node)
    {
        XenoArtifactNodeComponent nodeComponent = node;
        if (nodeComponent.Attached == null)
        {
            nodeComponent.ResearchValue = 0;
            return;
        }

        var artifact = _xenoArtifactQuery.Get(GetEntity(nodeComponent.Attached.Value));

        var nonactiveNodes = GetActiveNodes(artifact);
        var durabilityEffect = MathF.Pow((float)nodeComponent.Durability / nodeComponent.MaxDurability, 2);
        var durabilityMultiplier = nonactiveNodes.Contains(node)
            ? 1f - durabilityEffect
            : 1f + durabilityEffect;

        var predecessorNodes = GetPredecessorNodes((artifact, artifact), node);
        nodeComponent.ResearchValue = (int)(Math.Pow(1.25, Math.Pow(predecessorNodes.Count, 1.5f)) * nodeComponent.BasePointValue * durabilityMultiplier);
    }
}
