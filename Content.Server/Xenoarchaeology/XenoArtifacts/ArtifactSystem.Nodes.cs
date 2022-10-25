using System.Linq;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public sealed partial class ArtifactSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    /// <summary>
    /// Randomize a given artifact.
    /// </summary>
    public void RandomizeArtifact(ArtifactComponent component)
    {
        var nodeAmount = _random.Next(component.NodesMin, component.NodesMax);
        component.RandomSeed = _random.Next();
        component.NodeTree = new ArtifactTree();
        GenerateArtifactNodeTree(ref component.NodeTree, nodeAmount);
        EnterNode(component.Owner, ref component.NodeTree.StartNode, component);
    }

    /// <summary>
    /// Generate an Artifact tree with fully developed nodes.
    /// </summary>
    /// <param name="tree">The tree being generated.</param>
    /// <param name="nodeAmount">The amount of nodes it has.</param>
    public void GenerateArtifactNodeTree(ref ArtifactTree tree, int nodeAmount)
    {
        if (nodeAmount < 1)
        {
            Logger.Error($"nodeAmount {nodeAmount} is less than 1. Aborting artifact tree generation.");
            return;
        }

        var uninitializedNodes = new List<ArtifactNode> { new() };
        tree.StartNode = uninitializedNodes.First(); //the first node

        while (uninitializedNodes.Any())
        {
            GenerateNode(ref uninitializedNodes, ref tree, nodeAmount);
        }
    }

    /// <summary>
    /// Generate an individual node on the tree.
    /// </summary>
    private void GenerateNode(ref List<ArtifactNode> uninitializedNodes, ref ArtifactTree tree, int targetNodeAmount)
    {
        if (!uninitializedNodes.Any())
            return;

        var node = uninitializedNodes.First();
        uninitializedNodes.Remove(node);

        node.Id = $"node-{_random.Next(0, 10000)}";

        //Generate the connected nodes
        var maxEdges = targetNodeAmount - tree.AllNodes.Count - uninitializedNodes.Count - 1;
        var minEdges = Math.Min(maxEdges, 1);
        var edgeAmount = _random.Next(minEdges, maxEdges);

        for (var i = 0; i < edgeAmount; i++)
        {
            var neighbor = new ArtifactNode
            {
                Depth = node.Depth + 1
            };
            node.Edges.Add(neighbor);
            neighbor.Edges.Add(node);

            uninitializedNodes.Add(neighbor);
        }

        //create trigger here
        var triggerProto = _random.Pick(_prototype.EnumeratePrototypes<ArtifactTriggerPrototype>().ToList());
        node.Trigger = triggerProto;

        //TODO: make some kind of weighted system based on depth.
        var effectProto = _random.Pick(_prototype.EnumeratePrototypes<ArtifactEffectPrototype>().ToList());
        node.Effect = effectProto;

        tree.AllNodes.Add(node);
    }

    /// <summary>
    /// Enter a node: attach the relevant components
    /// </summary>
    public void EnterNode(EntityUid uid, ref ArtifactNode node, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.CurrentNode != null)
        {
            ExitNode(uid, component);
        }

        component.CurrentNode = node;
        node.Discovered = true;

        if (node.Trigger != null)
        {
            foreach (var (name, entry) in node.Trigger.Components)
            {
                var reg = _componentFactory.GetRegistration(name);
                var comp = (Component) _componentFactory.GetComponent(reg);
                comp.Owner = uid;

                var temp = (object) comp;
                _serialization.Copy(entry.Component, ref temp);
                EntityManager.AddComponent(uid, (Component) temp!, true);
            }
        }

        if (node.Effect != null)
        {
            var allComponents = node.Effect.Components.Concat(node.Effect.PermanentComponents);
            foreach (var (name, entry) in allComponents)
            {
                var reg = _componentFactory.GetRegistration(name);
                var comp = (Component) _componentFactory.GetComponent(reg);
                comp.Owner = uid;

                var temp = (object) comp;
                _serialization.Copy(entry.Component, ref temp);
                EntityManager.AddComponent(uid, (Component) temp!, true);
            }
        }

        RaiseLocalEvent(uid, new ArtifactNodeEnteredEvent(component.RandomSeed));
    }

    /// <summary>
    /// Exit a node: remove the relevant components.
    /// </summary>
    public void ExitNode(EntityUid uid, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var node = component.CurrentNode;
        if (node == null)
            return;

        if (node.Trigger != null)
        {
            foreach (var name in node.Trigger.Components.Keys)
            {
                var comp = _componentFactory.GetRegistration(name);
                RemCompDeferred(uid, comp.Type);
            }
        }

        if (node.Effect != null)
        {
            foreach (var name in node.Effect.Components.Keys)
            {
                var comp = _componentFactory.GetRegistration(name);
                RemCompDeferred(uid, comp.Type);
            }
        }

        component.CurrentNode = null;
    }
}
