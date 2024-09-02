using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking;
using Content.Server.Power.EntitySystems;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.CCVar;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public sealed partial class ArtifactSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactComponent, PriceCalculationEvent>(GetPrice);

        InitializeCommands();
        InitializeActions();
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
        args.Price += (GetResearchPointValue(uid, component) + component.ConsumedPoints) * component.PriceMultiplier;
    }

    /// <summary>
    /// Calculates how many research points the artifact is worth
    /// </summary>
    /// <remarks>
    /// General balancing (for fully unlocked artifacts):
    /// Simple (1-2 Nodes): ~10K
    /// Medium (5-8 Nodes): ~30-40K
    /// Complex (7-12 Nodes): ~60-80K
    ///
    /// Simple artifacts should be enough to unlock a few techs.
    /// Medium should get you partway through a tree.
    /// Complex should get you through a full tree and then some.
    /// </remarks>
    public int GetResearchPointValue(EntityUid uid, ArtifactComponent? component = null, bool getMaxPrice = false)
    {
        if (!Resolve(uid, ref component))
            return 0;

        var sumValue = component.NodeTree.Sum(n => GetNodePointValue(n, component, getMaxPrice));
        var fullyExploredBonus = component.NodeTree.All(x => x.Triggered) || getMaxPrice ? 1.25f : 1;

        return (int) (sumValue * fullyExploredBonus) - component.ConsumedPoints;
    }

    /// <summary>
    /// Adjusts how many points on the artifact have been consumed
    /// </summary>
    public void AdjustConsumedPoints(EntityUid uid, int amount, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.ConsumedPoints += amount;
    }

    /// <summary>
    /// Sets whether or not the artifact is suppressed,
    /// preventing it from activating
    /// </summary>
    public void SetIsSuppressed(EntityUid uid, bool suppressed, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.IsSuppressed = suppressed;
    }

    /// <summary>
    /// Gets the point value for an individual node
    /// </summary>
    private float GetNodePointValue(ArtifactNode node, ArtifactComponent component, bool getMaxPrice = false)
    {
        var valueDeduction = 1f;
        if (!getMaxPrice)
        {
            if (!node.Discovered)
                return 0;

            valueDeduction = !node.Triggered ? 0.25f : 1;
        }

        var triggerProto = _prototype.Index<ArtifactTriggerPrototype>(node.Trigger);
        var effectProto = _prototype.Index<ArtifactEffectPrototype>(node.Effect);

        var nodeDanger = (node.Depth + effectProto.TargetDepth + triggerProto.TargetDepth) / 3;
        return component.PointsPerNode * MathF.Pow(component.PointDangerMultiplier, nodeDanger) * valueDeduction;
    }

    /// <summary>
    /// Randomize a given artifact.
    /// </summary>
    [PublicAPI]
    public void RandomizeArtifact(EntityUid uid, ArtifactComponent component)
    {
        var nodeAmount = _random.Next(component.NodesMin, component.NodesMax);

        GenerateArtifactNodeTree(uid, component.NodeTree, nodeAmount);
        var firstNode = GetRootNode(component.NodeTree);
        EnterNode(uid, ref firstNode, component);
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
        if (component.CurrentNodeId == null)
            return;

        _audio.PlayPvs(component.ActivationSound, uid);
        component.LastActivationTime = _gameTiming.CurTime;

        var ev = new ArtifactActivatedEvent
        {
            Activator = user
        };
        RaiseLocalEvent(uid, ev, true);

        var currentNode = GetNodeFromId(component.CurrentNodeId.Value, component);

        currentNode.Triggered = true;
        if (currentNode.Edges.Count == 0)
            return;

        var newNode = GetNewNode(uid, component);
        if (newNode == null)
            return;

        EnterNode(uid, ref newNode, component);
    }

    private ArtifactNode? GetNewNode(EntityUid uid, ArtifactComponent component)
    {
        if (component.CurrentNodeId == null)
            return null;

        var currentNode = GetNodeFromId(component.CurrentNodeId.Value, component);

        var allNodes = currentNode.Edges;
        Log.Debug($"our node: {currentNode.Id}");
        Log.Debug($"other nodes: {string.Join(", ", allNodes)}");

        if (TryComp<BiasedArtifactComponent>(uid, out var bias) &&
            TryComp<TraversalDistorterComponent>(bias.Provider, out var trav) &&
            this.IsPowered(bias.Provider, EntityManager))
        {
            switch (trav.BiasDirection)
            {
                case BiasDirection.Up:
                    var upNodes = allNodes.Where(x => GetNodeFromId(x, component).Depth < currentNode.Depth).ToHashSet();
                    if (upNodes.Count != 0)
                        allNodes = upNodes;
                    break;
                case BiasDirection.Down:
                    var downNodes = allNodes.Where(x => GetNodeFromId(x, component).Depth > currentNode.Depth).ToHashSet();
                    if (downNodes.Count != 0)
                        allNodes = downNodes;
                    break;
            }
        }

        var undiscoveredNodes = allNodes.Where(x => !GetNodeFromId(x, component).Discovered).ToList();
        Log.Debug($"Undiscovered nodes: {string.Join(", ", undiscoveredNodes)}");
        var newNode = _random.Pick(allNodes);

        if (undiscoveredNodes.Count != 0 && _random.Prob(0.75f))
        {
            newNode = _random.Pick(undiscoveredNodes);
        }

        Log.Debug($"Going to node {newNode}");

        return GetNodeFromId(newNode, component);
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
    public bool TryGetNodeData<T>(EntityUid uid, string key, [NotNullWhen(true)] out T? data, ArtifactComponent? component = null)
    {
        data = default;

        if (!Resolve(uid, ref component))
            return false;

        if (component.CurrentNodeId == null)
            return false;
        var currentNode = GetNodeFromId(component.CurrentNodeId.Value, component);

        if (currentNode.NodeData.TryGetValue(key, out var dat) && dat is T value)
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

        if (component.CurrentNodeId == null)
            return;
        var currentNode = GetNodeFromId(component.CurrentNodeId.Value, component);

        currentNode.NodeData[key] = value;
    }

    /// <summary>
    /// Gets the base node (depth 0) of an artifact's node graph
    /// </summary>
    /// <param name="allNodes"></param>
    /// <returns></returns>
    public ArtifactNode GetRootNode(List<ArtifactNode> allNodes)
    {
        return allNodes.First(n => n.Depth == 0);
    }
}
