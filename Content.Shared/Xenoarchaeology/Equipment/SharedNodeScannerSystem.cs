using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.NameIdentifier;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Timing;
using NodeScannerComponent = Content.Shared.Xenoarchaeology.Equipment.Components.NodeScannerComponent;

namespace Content.Shared.Xenoarchaeology.Equipment;

public abstract class SharedNodeScannerSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedXenoArtifactSystem _artifact = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NodeScannerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
        SubscribeLocalEvent<NodeScannerComponent, GetVerbsEvent<UtilityVerb>>(AddScanVerb);
    }

    private void OnBeforeRangedInteract(EntityUid uid, NodeScannerComponent component, BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        Entity<XenoArtifactUnlockingComponent?> unlockingEnt = TryComp<XenoArtifactUnlockingComponent>(target, out var unlockingComponent)
            ? (target, unlockingComponent)
            : (target, null);

        TryMakeActiveNodesSnapshot((uid, component), unlockingEnt, args.User);

        args.Handled = true;
    }

    private void AddScanVerb(EntityUid uid, NodeScannerComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (!TryComp<XenoArtifactUnlockingComponent>(args.Target, out var unlockingComponent))
            return;

        var verb = new UtilityVerb
        {
            Act = () =>
            {
                TryMakeActiveNodesSnapshot((uid, component), (args.Target, unlockingComponent), args.User);
            },
            Text = Loc.GetString("node-scan-tooltip")
        };

        args.Verbs.Add(verb);
    }

    private void TryMakeActiveNodesSnapshot(
        Entity<NodeScannerComponent> device,
        Entity<XenoArtifactUnlockingComponent?> unlockingEnt,
        EntityUid actor
    )
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (TryComp(device, out UseDelayComponent? useDelay)
            && !_useDelay.TryResetDelay((device, useDelay), true))
            return;

        if (!TryComp<XenoArtifactComponent>(unlockingEnt.Owner, out var artifactComponent))
            return;

        TryOpenUi(device, actor);

        HashSet<string> triggeredNodeNames;

        if (unlockingEnt.Comp == null)
        {
            triggeredNodeNames = new HashSet<string>();
        }
        else
        {
            var triggeredIndexes = unlockingEnt.Comp.TriggeredNodeIndexes;
            triggeredNodeNames = new HashSet<string>(triggeredIndexes.Count);

            foreach (var triggeredIndex in triggeredIndexes)
            {
                var node = _artifact.GetNode((unlockingEnt.Owner, artifactComponent), triggeredIndex);
                var triggeredNodeName = (CompOrNull<NameIdentifierComponent>(node)?.Identifier ?? 0).ToString("D3");
                triggeredNodeNames.Add(triggeredNodeName);
            }
        }

        if (!device.Comp.TriggeredNodesSnapshot.SequenceEqual(triggeredNodeNames))
        {
            device.Comp.TriggeredNodesSnapshot = triggeredNodeNames;

            Dirty(device);
        }
    }

    protected abstract void TryOpenUi(Entity<NodeScannerComponent> device, EntityUid actor);
}
