using System.Linq;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public sealed partial class ArtifactSystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    private const int MaxEdgesPerNode = 4;

    private readonly HashSet<int> _usedNodeIds = new();

    /// <summary>
    /// Generate an Artifact tree with fully developed nodes.
    /// </summary>
    /// <param name="artifact"></param>
    /// <param name="allNodes"></param>
    /// <param name="nodesToCreate">The amount of nodes it has.</param>
    private void GenerateArtifactNodeTree(EntityUid artifact, List<ArtifactNode> allNodes, int nodesToCreate)
    {
        if (nodesToCreate < 1)
        {
            Log.Error($"nodesToCreate {nodesToCreate} is less than 1. Aborting artifact tree generation.");
            return;
        }

        _usedNodeIds.Clear();

        var uninitializedNodes = new List<ArtifactNode> { new(){ Id = GetValidNodeId() } };
        var createdNodes = 1;

        while (uninitializedNodes.Count > 0)
        {
            var node = uninitializedNodes[0];
            uninitializedNodes.Remove(node);

            node.Trigger = GetRandomTrigger(artifact, ref node);
            node.Effect = GetRandomEffect(artifact, ref node);

            var maxChildren = _random.Next(1, MaxEdgesPerNode - 1);

            for (var i = 0; i < maxChildren; i++)
            {
                if (nodesToCreate <= createdNodes)
                {
                    break;
                }

                var child = new ArtifactNode {Id = GetValidNodeId(), Depth = node.Depth + 1};
                node.Edges.Add(child.Id);
                child.Edges.Add(node.Id);

                uninitializedNodes.Add(child);
                createdNodes++;
            }

            allNodes.Add(node);
        }
    }

    private int GetValidNodeId()
    {
        var id = _random.Next(100, 1000);
        while (_usedNodeIds.Contains(id))
        {
            id = _random.Next(100, 1000);
        }

        _usedNodeIds.Add(id);

        return id;
    }

    //yeah these two functions are near duplicates but i don't
    //want to implement an interface or abstract parent

    private string GetRandomTrigger(EntityUid artifact, ref ArtifactNode node)
    {
        var allTriggers = _prototype.EnumeratePrototypes<ArtifactTriggerPrototype>()
            .Where(x => _whitelistSystem.IsWhitelistPassOrNull(x.Whitelist, artifact) &&
            _whitelistSystem.IsBlacklistFailOrNull(x.Blacklist, artifact)).ToList();
        var validDepth = allTriggers.Select(x => x.TargetDepth).Distinct().ToList();

        var weights = GetDepthWeights(validDepth, node.Depth);
        var selectedRandomTargetDepth = GetRandomTargetDepth(weights);
        var targetTriggers = allTriggers
            .Where(x => x.TargetDepth == selectedRandomTargetDepth).ToList();

        return _random.Pick(targetTriggers).ID;
    }

    private string GetRandomEffect(EntityUid artifact, ref ArtifactNode node)
    {
        var allEffects = _prototype.EnumeratePrototypes<ArtifactEffectPrototype>()
            .Where(x => _whitelistSystem.IsWhitelistPassOrNull(x.Whitelist, artifact) &&
            _whitelistSystem.IsBlacklistFailOrNull(x.Blacklist, artifact)).ToList();
        var validDepth = allEffects.Select(x => x.TargetDepth).Distinct().ToList();

        var weights = GetDepthWeights(validDepth, node.Depth);
        var selectedRandomTargetDepth = GetRandomTargetDepth(weights);
        var targetEffects = allEffects
            .Where(x => x.TargetDepth == selectedRandomTargetDepth).ToList();

        return _random.Pick(targetEffects).ID;
    }

    /// <remarks>
    /// The goal is that the depth that is closest to targetDepth has the highest chance of appearing.
    /// The issue is that we also want some variance, so levels that are +/- 1 should also have a
    /// decent shot of appearing. This function should probably get some tweaking at some point.
    /// </remarks>
    private Dictionary<int, float> GetDepthWeights(IEnumerable<int> depths, int targetDepth)
    {
        // this function is just a normal distribution with a
        // mean of target depth and standard deviation of 0.75
        var weights = new Dictionary<int, float>();
        foreach (var d in depths)
        {
            var w = 10f / (0.75f * MathF.Sqrt(2 * MathF.PI)) * MathF.Pow(MathF.E, -MathF.Pow((d - targetDepth) / 0.75f, 2));
            weights.Add(d, w);
        }
        return weights;
    }

    /// <summary>
    /// Uses a weighted random system to get a random depth.
    /// </summary>
    private int GetRandomTargetDepth(Dictionary<int, float> weights)
    {
        var sum = weights.Values.Sum();
        var accumulated = 0f;

        var rand = _random.NextFloat() * sum;

        foreach (var (key, weight) in weights)
        {
            accumulated += weight;

            if (accumulated >= rand)
            {
                return key;
            }
        }

        return _random.Pick(weights.Keys); //shouldn't happen
    }

    /// <summary>
    /// Enter a node: attach the relevant components
    /// </summary>
    private void EnterNode(EntityUid uid, ref ArtifactNode node, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.CurrentNodeId != null)
        {
            ExitNode(uid, component);
        }

        component.CurrentNodeId = node.Id;

        var trigger = _prototype.Index<ArtifactTriggerPrototype>(node.Trigger);
        var effect = _prototype.Index<ArtifactEffectPrototype>(node.Effect);

        var allComponents = effect.Components.Concat(effect.PermanentComponents).Concat(trigger.Components);
        foreach (var (name, entry) in allComponents)
        {
            var reg = _componentFactory.GetRegistration(name);

            if (node.Discovered && EntityManager.HasComponent(uid, reg.Type))
            {
                // Don't re-add permanent components unless this is the first time you've entered this node
                if (effect.PermanentComponents.ContainsKey(name))
                    continue;

                EntityManager.RemoveComponent(uid, reg.Type);
            }

            var comp = (Component) _componentFactory.GetComponent(reg);
            comp.Owner = uid;

            var temp = (object) comp;
            _serialization.CopyTo(entry.Component, ref temp);
            EntityManager.RemoveComponent(uid, temp!.GetType());
            EntityManager.AddComponent(uid, (Component) temp!);
        }

        node.Discovered = true;
        RaiseLocalEvent(uid, new ArtifactNodeEnteredEvent(component.CurrentNodeId.Value));
    }

    /// <summary>
    /// Exit a node: remove the relevant components.
    /// </summary>
    private void ExitNode(EntityUid uid, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.CurrentNodeId == null)
            return;
        var currentNode = GetNodeFromId(component.CurrentNodeId.Value, component);

        var trigger = _prototype.Index<ArtifactTriggerPrototype>(currentNode.Trigger);
        var effect = _prototype.Index<ArtifactEffectPrototype>(currentNode.Effect);

        var entityPrototype = MetaData(uid).EntityPrototype;
        var toRemove = effect.Components.Keys.Concat(trigger.Components.Keys).ToList();

        foreach (var name in toRemove)
        {
            // if the entity prototype contained the component originally
            if (entityPrototype?.Components.TryGetComponent(name, out var entry) ?? false)
            {
                var comp = (Component) _componentFactory.GetComponent(name);
                comp.Owner = uid;
                var temp = (object) comp;
                _serialization.CopyTo(entry, ref temp);
                EntityManager.RemoveComponent(uid, temp!.GetType());
                EntityManager.AddComponent(uid, (Component) temp);
                continue;
            }

            EntityManager.RemoveComponentDeferred(uid, _componentFactory.GetRegistration(name).Type);
        }
        component.CurrentNodeId = null;
    }

    [PublicAPI]
    public ArtifactNode GetNodeFromId(int id, ArtifactComponent component)
    {
        return component.NodeTree.First(x => x.Id == id);
    }

    [PublicAPI]
    public ArtifactNode GetNodeFromId(int id, IEnumerable<ArtifactNode> nodes)
    {
        return nodes.First(x => x.Id == id);
    }
}
