using Content.Shared.EntityTable;
using Content.Shared.NameIdentifier;
using Content.Shared.Random.Helpers;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Modifiers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.EntityTable.EntitySelectors;
using System.Linq;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Random;

namespace Content.Shared.Xenoarchaeology.Artifact;

public abstract partial class SharedXenoArtifactSystem
{

    private static readonly Enum[] OnInitEffectModifiers = [XenoArtifactEffectModifier.Durability];
    private static readonly PlacementBudgetDistributionStrategyBase[] BudgetDistributionStrategies =
    [
        new AllInOnePlacementBudgetDistributionStrategy(),
        new NormalPlacementBudgetDistributionStrategy(),
        new OffsettingPlacementBudgetDistributionStrategy()
    ];

    [Dependency] private EntityTableSystem _entityTable =  default!;

    [Dependency] private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery = default!;
    [Dependency] private EntityQuery<XenoArtifactNodeComponent> _nodeQuery = default!;

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
        SetNodeDurability((ent, ent), nodeComponent.MaxDurability);
    }

    [SubscribeLocalEvent]
    private void OnAmplify(Entity<XenoArtifactNodeComponent> ent, ref XenoArtifactCollectEffectModificationsOnInitEvent args)
    {
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Durability, out var durabilityChange))
        {
            ent.Comp.Durability = Math.Max(1, (int)durabilityChange.Modify(ent.Comp.Durability));
            Dirty(ent);
        }
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
    public Entity<XenoArtifactNodeComponent>? CreateNode(
        Entity<XenoArtifactComponent> ent,
        EntProtoId triggerProtoId,
        EntityTableSelector effects,
        int depth = 0
    )
    {
        EntProtoId? effect = _entityTable.GetSpawns(effects)
                                         .FirstOrDefault();
        if (effect == null)
            return null;

        var trigger = ProtoMan.Index(triggerProtoId);
        return CreateNode(ent, effect.Value, trigger, depth);
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

    public Entity<XenoArtifactNodeComponent>? CreateNode(
    Entity<XenoArtifactComponent> ent,
    List<Entity<XenoArtifactNodeComponent>> directPredecessors,
    EntityTableSelector triggers,
    EntityTableSelector effects,
    int depth = 0
)
    {
        // step 1 - pick trigger by budget
        var predecessorBudgetSum = 0;
        if (directPredecessors.Count > 0)
            predecessorBudgetSum = directPredecessors.Sum(x => x.Comp.Budget);

        const int perDepthAdditionalBudget = 2000;
        var virtualNodeAdditionalBudget = perDepthAdditionalBudget * depth;
        var virtualNodeBudget = predecessorBudgetSum + virtualNodeAdditionalBudget;

        var fittingTriggersByWeight = new Dictionary<XenoArchTriggerPrototype, float>();
        foreach (var (t, weight) in triggers)
        {
            var budgetRange = t.BudgetRange;
            if (budgetRange.Min <= virtualNodeBudget && budgetRange.Max >= virtualNodeBudget)
                fittingTriggersByWeight.Add(t, weight);
        }

        if (fittingTriggersByWeight.Count == 0)
            return null;

        var trigger = RobustRandom.PickAndTake(fittingTriggersByWeight);

        var actualBudget = predecessorBudgetSum + trigger.TriggerBudget;

        // pick effect based on effect ranges and actual node budget.
        Dictionary<(EntityPrototype Prototype, XenoArtifactNodeBudgetComponent Budget), float> fittingEffectsByWeight = new();
        foreach (var (e, weight) in effects)
        {
            if (!e.Components.TryGetComponent<XenoArtifactNodeBudgetComponent>(Factory, out var nodeBudgetComp))
                continue;

            var budgetRange = nodeBudgetComp.BudgetRange;
            if (budgetRange.Min <= actualBudget && budgetRange.Max >= actualBudget)
                fittingEffectsByWeight.Add((e, nodeBudgetComp), weight);
        }

        if (fittingEffectsByWeight.Count == 0)
            return null;

        var effect = RobustRandom.PickAndTake(fittingEffectsByWeight);

        triggers.Remove(trigger);

        AddNode((ent, ent), effect.Prototype, out var nodeEnt, dirty: false);
        DebugTools.Assert(nodeEnt.HasValue, "Failed to create node on artifact.");

        var nodeComponent = nodeEnt.Value.Comp;
        nodeComponent.Depth = depth;
        nodeComponent.Budget = actualBudget;

        var budget = EnsureComp<XenoArtifactNodeBudgetComponent>(nodeEnt.Value);

        ApplyActualBudgetPlacement((nodeEnt.Value, budget), actualBudget);

        XenoArtifactEffectsModifications onInitAmplifications = new();
        foreach (var onInitEffectModifier in OnInitEffectModifiers)
        {
            if (effect.Budget.ModifyBy.Dictionary.TryGetValue(onInitEffectModifier, out var value))
            {
                onInitAmplifications.Dictionary.Add(onInitEffectModifier, value);
            }
        }

        nodeComponent.TriggerTip = trigger.Tip;
        EntityManager.AddComponents(nodeEnt.Value, trigger.Components);

        if (!onInitAmplifications.IsEmpty)
        {
            var ev = new XenoArtifactCollectEffectModificationsOnInitEvent(onInitAmplifications);
            RaiseLocalEvent(nodeEnt.Value, ref ev);
        }

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

    private void ApplyActualBudgetPlacement(Entity<XenoArtifactNodeBudgetComponent> budgetEnt, int actualBudget)
    {
        // Calculate where node is placed inside budget range.
        // For example for range  1000 - 2000 node with 2000 actual budget will be at '1'=100%,
        // node with 1500 will be at '0.5'=50%, with 500 at '-0.5'=-50%
        // placement in budget range affects how node modifier affects power of effect.
        // Negative means lowering power, positive improved power
        XenoArtifactNodeBudgetComponent budget = budgetEnt;
        var halfRange = (float)(budget.BudgetRange.Max + budget.BudgetRange.Min) / 2;
        var placementInBudgetRange = (actualBudget - halfRange) / halfRange;

        var pickedStrategy = RobustRandom.Pick(BudgetDistributionStrategies);
        var keys = budget.ModifyBy.Dictionary.Keys;
        var distribution = pickedStrategy.Distribute(placementInBudgetRange, keys, RobustRandom);

        foreach (var (key, provider) in budget.ModifyBy.Dictionary)
        {
            if (provider is IBudgetPlacementAwareModifier budgetPlacementAware && distribution.TryGetValue(key, out var share))
            {
                budgetPlacementAware.PlacementInBudget = share;
            }
        }

        Dirty(budgetEnt, budget);
    }

    private XenoArtifactEffectsModifications GetBudgetNodeEffectModifications(Entity<XenoArtifactNodeComponent> node)
    {
        var currentAmplification = new XenoArtifactEffectsModifications();
        if (TryComp<XenoArtifactNodeBudgetComponent>(node, out var budget))
            return budget.ModifyBy;

        return currentAmplification;

    }
}

/// <summary>
/// XenoArtifact effect modifiers, can be used to affect aspects of effects, increasing or decreasing its power.
/// </summary>
[Serializable, NetSerializable]
public enum XenoArtifactEffectModifier
{
    /// <summary>
    /// Increase or decrease node durability.
    /// </summary>
    Durability,
    /// <summary>
    /// Increase or decrease range in which effect will work. Specific result depends on effect.
    /// </summary>
    Range,
    /// <summary>
    /// Increase or decrease duration of effect.
    /// </summary>
    Duration,
    /// <summary>
    /// Increase effect power - actual effect depends on exact artifact effect.
    /// </summary>
    Power,
}
/// <summary>
/// Event for collecting artifact node effects modifications on node init.
/// Can be used to modify static data, such as durability, which should not be re-evaluated on each activation.
/// </summary>
/// <param name="Modifications">
/// Collection of effect modification keys (aspects of artifact effect behaviour), with respective modification value.
/// </param>
[ByRefEvent]
public record struct XenoArtifactCollectEffectModificationsOnInitEvent(XenoArtifactEffectsModifications Modifications);

/// <summary>
/// Event of collecting artifact node effects modifications on node activation.
/// Can be used to modify node effect from node budget (deeper and more inter-connected nodes should be more powerful)
/// or from other nodes (meta-nodes that are affecting other nodes effects, changing range, amount of produced items, etc).
/// Is called on both all active nodes and on artifact itself.
/// </summary>
/// <param name="Modifications">
/// Collection of effect modification keys (aspects of artifact effect behaviour), with respective modification value.
/// </param>
[ByRefEvent]
public record struct XenoArtifactCollectEffectModificationsOnActivationEvent(XenoArtifactEffectsModifications Modifications);
