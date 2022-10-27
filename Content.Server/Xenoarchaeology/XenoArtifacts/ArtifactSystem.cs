using System.Linq;
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
    }

    private void OnInit(EntityUid uid, ArtifactComponent component, MapInitEvent args)
    {
        RandomizeArtifact(component);
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
