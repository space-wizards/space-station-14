using Content.Shared.Interaction;
using Content.Shared.Timing.Systems;
using Content.Shared.Verbs;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary> Controls behaviour of artifact node scanner device. </summary>
public sealed partial class NodeScannerSystem : EntitySystem
{
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedXenoArtifactSystem _artifact = default!;

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        var scannerQuery = EntityQueryEnumerator<NodeScannerConnectedComponent, NodeScannerComponent, TransformComponent>();
        while (scannerQuery.MoveNext(out var uid, out var connected, out var scanner, out var transform))
        {
            if (connected.NextUpdate > _timing.CurTime)
                continue;

            connected.NextUpdate = _timing.CurTime + connected.LinkUpdateInterval;

            var attachedArtifact = connected.AttachedTo;
            var artifactCoordinates = Transform(attachedArtifact).Coordinates;
            if (!_transform.InRange(artifactCoordinates, transform.Coordinates, scanner.MaxLinkedRange))
            {
                //scanner is too far, disconnect
                RemCompDeferred(uid, connected);
            }
        }
    }

    /// <summary> Disconnect if artifact is destroyed </summary>
    [SubscribeLocalEvent]
    private void OnArtifactRemoved(Entity<NodeScannerConnectedComponent> ent, ref XenoArtifactDestroyedEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }

    /// <summary> Detach if scanner is disconnected, or destroyed. </summary>
    [SubscribeLocalEvent]
    private void OnScannerRemoved(Entity<NodeScannerConnectedComponent> ent, ref ComponentRemove args)
    {
        var artifact = ent.Comp.AttachedTo;
        if (!TerminatingOrDeleted(artifact))
        {
            _artifact.TryDetachEntity((artifact, null), ent);
        }
    }

    /// <summary> Attach scanner if target is fitting. </summary>
    [SubscribeLocalEvent]
    private void OnBeforeRangedInteract(Entity<NodeScannerComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target || !HasComp<XenoArtifactComponent>(target))
            return;

        Entity<XenoArtifactUnlockingComponent?> unlockingEnt = TryComp<XenoArtifactUnlockingComponent>(target, out var unlockingComponent)
            ? (target, unlockingComponent)
            : (target, null);

        Attach(ent, unlockingEnt, args.User);

        args.Handled = true;
    }

    /// <summary> Add `scan` verb if target is fitting. </summary>
    [SubscribeLocalEvent]
    private void AddScanVerb(Entity<NodeScannerComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess)
            return;

        var target = args.Target;
        if (!TryComp<XenoArtifactUnlockingComponent>(target, out var unlockingComponent))
            return;

        var user = args.User;
        var verb = new UtilityVerb
        {
            Act = () => Attach(ent, (target, unlockingComponent), user),
            Text = Loc.GetString("node-scan-tooltip")
        };

        args.Verbs.Add(verb);
    }

    private void Attach(
        Entity<NodeScannerComponent> device,
        Entity<XenoArtifactUnlockingComponent?> unlockingEnt,
        EntityUid actor
    )
    {
        if (!_useDelay.TryResetDelay(device.Owner, true))
            return;

        var connected = EnsureComp<NodeScannerConnectedComponent>(device);

        EntityUid artifact = unlockingEnt;
        if (connected.AttachedTo != artifact)
        {
            // Remove connection from previous scanner.
            if (connected.AttachedTo.Valid)
                _artifact.TryDetachEntity(connected.AttachedTo, device);

            connected.AttachedTo = artifact;
            Dirty(device, connected);
            _artifact.TryAttachEntity(artifact, device);
        }

        _ui.TryOpenUi((device, null), NodeScannerUiKey.Key, actor, predicted: true);
    }
}
