using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public sealed partial class ArtifactSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ArtifactComponent, PriceCalculationEvent>(GetPrice);
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

    private double GetNodePrice(ArtifactNode node)
    {
        if (!node.Discovered) //no money for undiscovered nodes.
            return 0;

        //quarter price if not triggered
        var priceMultiplier = node.Triggered ? 1f : 0.25f;
        //the danger is the average of node depth, effect danger, and trigger danger.
        var nodeDanger = (node.Depth + node.Effect.TargetDepth + node.Trigger.TargetDepth) / 3;

        var price = MathF.Pow(2f, nodeDanger) * 500 * priceMultiplier;
        return price;
    }

    /// <summary>
    /// Randomize a given artifact.
    /// </summary>
    [PublicAPI]
    public void RandomizeArtifact(ArtifactComponent component)
    {
        var nodeAmount = _random.Next(component.NodesMin, component.NodesMax);
        component.RandomSeed = _random.Next();

        component.NodeTree = new ArtifactTree();

        GenerateArtifactNodeTree(ref component.NodeTree, nodeAmount);
        EnterNode(component.Owner, ref component.NodeTree.StartNode, component, false);
    }

    public bool TryActivateArtifact(EntityUid uid, EntityUid? user = null, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // check if artifact is under suppression field
        if (component.IsSuppressed)
            return false;

        // check if artifact isn't under cooldown
        var timeDif = _gameTiming.CurTime - component.LastActivationTime;
        if (timeDif.TotalSeconds < component.CooldownTime)
            return false;

        ForceActivateArtifact(uid, user, component);
        return true;
    }

    public void ForceActivateArtifact(EntityUid uid, EntityUid? user = null, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        component.LastActivationTime = _gameTiming.CurTime;

        var ev = new ArtifactActivatedEvent
        {
            Activator = user
        };
        RaiseLocalEvent(uid, ev, true);

        if (component.CurrentNode == null)
            return;
        component.CurrentNode.Triggered = true;
        if (component.CurrentNode.Edges.Any())
        {
            var newNode = _random.Pick(component.CurrentNode.Edges);
            EnterNode(uid, ref newNode, component);
        }
    }
}
