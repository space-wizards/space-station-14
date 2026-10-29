using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using Content.Shared.NameIdentifier;
using Content.Shared.Random.Helpers;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Xenoarchaeology.Artifact;

public abstract partial class SharedXenoArtifactSystem
{
    [Dependency] private EntityTableSystem _entityTable =  default!;

    [Dependency] private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery;
    [Dependency] private EntityQuery<XenoArtifactNodeComponent> _nodeQuery;

    private void InitializeNode()
    {
        SubscribeLocalEvent<XenoArtifactNodeComponent, MapInitEvent>(OnNodeMapInit);
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

    public void SetNodeUnlocked(Entity<XenoArtifactNodeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Attached is not { } artifact)
            return;

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
    public Entity<XenoArtifactNodeComponent> CreateNode(Entity<XenoArtifactComponent> ent, EntProtoId effect, EntityPrototype trigger, int depth = 0)
    {
        AddNode((ent, ent), effect, out var nodeEnt, dirty: false);
        DebugTools.Assert(nodeEnt.HasValue, "Failed to create node on artifact.");

        var nodeComponent = nodeEnt.Value.Comp;
        nodeComponent.Depth = depth;
        nodeComponent.TriggerTip = trigger.Name;

        EntityManager.AddComponents(nodeEnt.Value, trigger.Components);

        Dirty(nodeEnt.Value);
        return nodeEnt.Value;
    }

    /// <summary>
    /// Creates artifact node entity. To do this, we first calculated sum of node budget for
    /// direct predecessors, then we account for current depth, after which we pick
    /// trigger based on resulting budget. If we are successful, then we add actual
    /// budget from trigger and pick effect. If we fail to find proper effect or
    /// trigger - we return null, and consider further generation fruitless.
    /// </summary>
    protected Entity<XenoArtifactNodeComponent>? CreateNode(
        Entity<XenoArtifactComponent> ent,
        IReadOnlyCollection<Entity<XenoArtifactNodeComponent>> directPredecessors,
        TriggerPoolData triggerPool,
        int depth = 0
    )
    {
        // step 1 - accumulate predecessors budget
        float predecessorBudgetSum = 0;
        if (directPredecessors.Count > 0)
            predecessorBudgetSum = directPredecessors.Sum(x => x.Comp.ActualBudget);

        // step 2 - account for depth
        var virtualNodeAdditionalBudget = ent.Comp.PerDepthAdditionalBudgetBase * depth;
        var virtualNodeBudget = predecessorBudgetSum + virtualNodeAdditionalBudget;

        var pr = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        // step 3 - pick trigger using budget we accumulated
        EntProtoId? triggerProtoId;
        using (var _ = new TemporarilyAddToContext<float>(triggerPool.Context, HasArtifactBudgetInRangeCondition.BudgetContextKey, virtualNodeBudget))
        {
            triggerProtoId = _entityTable.GetFirstOrNull(ent.Comp.TriggersTable, pr, triggerPool.Context);
        }

        if (triggerProtoId == null)
            return null;

        var trigger = ProtoMan.Index(triggerProtoId.Value);
        float actualBudgetFromTrigger = 0;
        // TODO: invert condition and add warning + return null after all budgets are added to prototypes
        if (trigger.Components.TryGetComponent<XenoArtifactTriggerBudgetRangeComponent>(Factory, out var triggerBudget))
        {
            actualBudgetFromTrigger = triggerBudget.ActualBudget;
        }

        var actualBudget = predecessorBudgetSum + actualBudgetFromTrigger;

        // step 4 - pick effect based on effect ranges and actual node budget.
        var ctx = new EntityTableContext
        {
            { HasArtifactBudgetInRangeCondition.BudgetContextKey, actualBudget }
        };
        var effect = _entityTable.GetFirstOrNull(ent.Comp.EffectsTable, pr, ctx);
        if (effect == null)
            return null;

        // step 5 - prepare artifact and apply things related to budget, mark trigger as used
        triggerPool.AddTriggerAsUsed(triggerProtoId.Value);

        AddNode((ent, ent), effect.Value, out var nodeEntObj, dirty: false);
        DebugTools.Assert(nodeEntObj.HasValue, "Failed to create node on artifact.");

        var nodeEnt = nodeEntObj.Value;
        var nodeComponent = nodeEnt.Comp;
        nodeComponent.Depth = depth;
        nodeComponent.ActualBudget = actualBudget;

        nodeComponent.TriggerTip = trigger.Name;
        EntityManager.AddComponents(nodeEnt, trigger.Components);

        // TODO: apply actual placement in budget - pick attribute distribution strategy and add modifiers
        // TODO: Collect OnInitEffectModifiers that were applied by placement in budget
        // TODO: raise XenoArtifactCollectEffectModificationsOnInitEvent

        Dirty(nodeEnt);
        return nodeEnt;
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
                if (!_nodeQuery.TryComp(node, out var comp))
                    continue;

                outSegment.Add((node, comp));
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
    public List<List<Entity<XenoArtifactNodeComponent>>> GetSegmentsFromNodes(Entity<XenoArtifactComponent> ent, IReadOnlyCollection<Entity<XenoArtifactNodeComponent>> nodes)
    {
        var outSegments = new List<List<Entity<XenoArtifactNodeComponent>>>();
        foreach (var node in nodes)
        {
            var segment = new List<Entity<XenoArtifactNodeComponent>>();
            GetSegmentNodesRecursive(ent, node, segment, outSegments);

            if (segment.Count != 0)
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

        var artifact = _xenoArtifactQuery.Get(nodeComponent.Attached.Value);

        var nonactiveNodes = GetActiveNodes(artifact);
        var durabilityEffect = MathF.Pow((float)nodeComponent.Durability / nodeComponent.MaxDurability, 2);
        var durabilityMultiplier = nonactiveNodes.Contains(node)
            ? 1f - durabilityEffect
            : 1f + durabilityEffect;

        var predecessorNodes = GetPredecessorNodes((artifact, artifact), node);
        nodeComponent.ResearchValue = (int)(Math.Pow(1.25, Math.Pow(predecessorNodes.Count, 1.5f)) * nodeComponent.BasePointValue * durabilityMultiplier);
    }

    /// <summary>
    /// Structure that holds values that were temporarily added to <see cref="EntityTableContext"/>
    /// and should be removed on dispose call.
    /// </summary>
    /// <typeparam name="T">Type of value in context, for which we want to control lifetime.</typeparam>
    private readonly struct TemporarilyAddToContext<T> : IDisposable where T : notnull
    {
        private readonly EntityTableContext _context;
        private readonly EntityTableContextKey<T> _key;

        /// <summary>
        /// Creates marker struct that will add <paramref cref="value"/> to <paramref cref="context"/>
        /// immidiately, and will remove it during <see cref="Dispose"/> call.
        /// </summary>
        /// <param name="context">Context with which we want to control.</param>
        /// <param name="key">Key for storing and removing value.</param>
        /// <param name="value">Value to store now, and removed on disposal of this struct.</param>
        public TemporarilyAddToContext(EntityTableContext context, EntityTableContextKey<T> key, T value)
        {
            _context = context;
            _key = key;
            _context.SetData(_key, value);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _context.RemoveData(_key);
        }
    }

    /// <summary>
    /// Container that represents pool of XenoArtifact triggers.
    /// </summary>
    protected sealed class TriggerPoolData
    {
        private readonly HashSet<EntProtoId> _usedTriggers;

        /// <summary>
        /// Initializes pool with fixed size.
        /// </summary>
        public TriggerPoolData(int requestedSize)
        {
            _usedTriggers = new(requestedSize);
            Context = new EntityTableContext
            {
                { ExcludeEntitiesFromContextCondition.EntitiesToExclude, _usedTriggers }
            };
        }

        /// <summary>
        /// Context to be used. Already pre-filled with entities to be excluded, from picks,
        /// use <see cref="AddTriggerAsUsed"/> to add more entities.
        /// </summary>
        public readonly EntityTableContext Context;

        /// <summary>
        /// Marks trigger entity as already used, which excludes it from getting picked later.
        /// Returns true if trigger was marked, false if it was already marked before call.
        /// </summary>
        public bool AddTriggerAsUsed(EntProtoId trigger)
        {
            return _usedTriggers.Add(trigger);
        }

        /// <summary>
        /// List of already used trigger prototype ids.
        /// </summary>
        public IReadOnlyCollection<EntProtoId> UsedTriggers => _usedTriggers;
    }
}
