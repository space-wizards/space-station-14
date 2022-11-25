using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public sealed partial class ArtifactSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const int PricePerNode = 500;
    private const int PointsPerNode = 5000;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ArtifactComponent, PriceCalculationEvent>(GetPrice);

        InitializeCommands();
    }

    private void OnInit(EntityUid uid, ArtifactComponent component, MapInitEvent args)
    {
        RandomizeArtifact(component);
    }

    /// <summary>
    /// Calculates the price of an artifact based on
    /// how many nodes have been unlocked/triggered
    /// </summary>
    /// <remarks>
    /// General balancing (for fully unlocked artifacts):
    /// Simple (1-2 Nodes): 1-2K
    /// Medium (5-8 Nodes): 6-7K
    /// Complex (7-12 Nodes): 10-11K
    /// </remarks>
    private void GetPrice(EntityUid uid, ArtifactComponent component, ref PriceCalculationEvent args)
    {
        if (component.NodeTree == null)
            return;

        var price = component.NodeTree.AllNodes.Sum(GetNodePrice);

        // 25% bonus for fully exploring every node.
        var fullyExploredBonus = component.NodeTree.AllNodes.Any(x => !x.Triggered) ? 1 : 1.25f;

        args.Price =+ price * fullyExploredBonus;
    }

    private float GetNodePrice(ArtifactNode node)
    {
        if (!node.Discovered) //no money for undiscovered nodes.
            return 0;

        //quarter price if not triggered
        var priceMultiplier = node.Triggered ? 1f : 0.25f;
        //the danger is the average of node depth, effect danger, and trigger danger.
        var nodeDanger = (node.Depth + node.Effect.TargetDepth + node.Trigger.TargetDepth) / 3;

        var price = MathF.Pow(2f, nodeDanger) * PricePerNode * priceMultiplier;
        return price;
    }

    /// <summary>
    /// Calculates how many research points the artifact is worht
    /// </summary>
    /// <remarks>
    /// Rebalance this shit at some point. Definitely OP.
    /// </remarks>
    public int GetResearchPointValue(EntityUid uid, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.NodeTree == null)
            return 0;

        var sumValue = component.NodeTree.AllNodes.Sum(GetNodePointValue);
        var fullyExploredBonus = component.NodeTree.AllNodes.Any(x => !x.Triggered) ? 1 : 1.25f;

        var pointValue = (int) (sumValue * fullyExploredBonus);
        return pointValue;
    }

    private float GetNodePointValue(ArtifactNode node)
    {
        if (!node.Discovered)
            return 0;

        var valueDeduction = !node.Triggered ? 0.5f : 1;
        var nodeDanger = (node.Depth + node.Effect.TargetDepth + node.Trigger.TargetDepth) / 3;

        return (nodeDanger+1) * PointsPerNode * valueDeduction;
    }

    /// <summary>
    /// Randomize a given artifact.
    /// </summary>
    [PublicAPI]
    public void RandomizeArtifact(ArtifactComponent component)
    {
        var nodeAmount = _random.Next(component.NodesMin, component.NodesMax);

        component.NodeTree = new ArtifactTree();

        GenerateArtifactNodeTree(component.Owner, ref component.NodeTree, nodeAmount);
        EnterNode(component.Owner, ref component.NodeTree.StartNode, component);
    }

    /// <summary>
    /// Tries to activate the artifact
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="user"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public bool TryActivateArtifact(EntityUid uid, EntityUid? user = null, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // check if artifact is under suppression field
        if (component.IsSuppressed)
            return false;

        // check if artifact isn't under cooldown
        var timeDif = _gameTiming.CurTime - component.LastActivationTime;
        if (timeDif < component.CooldownTime)
            return false;

        ForceActivateArtifact(uid, user, component);
        return true;
    }

    /// <summary>
    /// Forces an artifact to activate
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="user"></param>
    /// <param name="component"></param>
    public void ForceActivateArtifact(EntityUid uid, EntityUid? user = null, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (component.CurrentNode == null)
            return;

        component.LastActivationTime = _gameTiming.CurTime;

        var ev = new ArtifactActivatedEvent
        {
            Activator = user
        };
        RaiseLocalEvent(uid, ev, true);

        component.CurrentNode.Triggered = true;
        if (component.CurrentNode.Edges.Any())
        {
            var newNode = GetNewNode(component);
            if (newNode == null)
                return;
            EnterNode(uid, ref newNode, component);
        }
    }

    private ArtifactNode? GetNewNode(ArtifactComponent component)
    {
        if (component.CurrentNode == null)
            return null;

        var allNodes = component.CurrentNode.Edges;

        if (TryComp<BiasedArtifactComponent>(component.Owner, out var bias) &&
            TryComp<TraversalDistorterComponent>(bias.Provider, out var trav) &&
            _random.Prob(trav.BiasChance) &&
            this.IsPowered(bias.Provider, EntityManager))
        {
            switch (trav.BiasDirection)
            {
                case BiasDirection.In:
                    var foo = allNodes.Where(x => x.Depth < component.CurrentNode.Depth).ToList();
                    if (foo.Any())
                        allNodes = foo;
                    break;
                case BiasDirection.Out:
                    var bar = allNodes.Where(x => x.Depth > component.CurrentNode.Depth).ToList();
                    if (bar.Any())
                        allNodes = bar;
                    break;
            }
        }

        var undiscoveredNodes = allNodes.Where(x => !x.Discovered).ToList();
        var newNode = _random.Pick(allNodes);
        if (undiscoveredNodes.Any() && _random.Prob(0.75f))
        {
            newNode = _random.Pick(undiscoveredNodes);
        }

        return newNode;
    }

    /// <summary>
    /// Try and get a data object from a node
    /// </summary>
    /// <param name="uid">The entity you're getting the data from</param>
    /// <param name="key">The data's key</param>
    /// <param name="data">The data you are trying to get.</param>
    /// <param name="component"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TryGetNodeData<T>(EntityUid uid, string key, [NotNullWhen(true)] out T data, ArtifactComponent? component = null)
    {
        data = default!;

        if (!Resolve(uid, ref component))
            return false;

        if (component.CurrentNode == null)
            return false;

        if (component.CurrentNode.NodeData.TryGetValue(key, out var dat) && dat is T value)
        {
            data = value;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets the node data to a certain value
    /// </summary>
    /// <param name="uid">The artifact</param>
    /// <param name="key">The key being set</param>
    /// <param name="value">The value it's being set to</param>
    /// <param name="component"></param>
    public void SetNodeData(EntityUid uid, string key, object value, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.CurrentNode == null)
            return;

        component.CurrentNode.NodeData[key] = value;
    }
}
