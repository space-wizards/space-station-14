using Content.Shared.Interaction;
using Content.Shared.NameIdentifier;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary> Controls behaviour of artifact node scanner device. </summary>
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

        TimeSpan? waitTime = null;
        HashSet<string> triggeredNodeNames;
        ArtifactState artifactState;
        if (unlockingEnt.Comp == null)
        {
            triggeredNodeNames = new HashSet<string>();
            var timeToUnlockAvailable = artifactComponent.NextUnlockTime - _timing.CurTime;
            if (timeToUnlockAvailable > TimeSpan.Zero)
            {
                artifactState = ArtifactState.Cooldown;
                waitTime = timeToUnlockAvailable;
            }
            else
            {
                artifactState = ArtifactState.Ready;
            }
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

            artifactState = ArtifactState.Unlocking;
            waitTime = _timing.CurTime - unlockingEnt.Comp.EndTime;
        }

        device.Comp.ArtifactState = artifactState;
        device.Comp.WaitTime = waitTime;
        device.Comp.TriggeredNodesSnapshot = triggeredNodeNames;
        device.Comp.ScannedAt = _timing.CurTime;

        Dirty(device);
    }

    protected abstract void TryOpenUi(Entity<NodeScannerComponent> device, EntityUid actor);
}
