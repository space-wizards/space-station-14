using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary> Controls behaviour of artifact node scanner device. </summary>
public sealed partial class NodeScannerSystem : EntitySystem
{
    private static readonly EntityTimerId LinkTimer = new("link");

    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NodeScannerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
        SubscribeLocalEvent<NodeScannerComponent, GetVerbsEvent<UtilityVerb>>(AddScanVerb);
        SubscribeLocalEvent<NodeScannerConnectedComponent, ComponentInit>(OnConnectedInit);
        SubscribeLocalEvent<NodeScannerConnectedComponent, ComponentHandleState>(OnConnectedHandleState);
        SubscribeLocalEvent<NodeScannerConnectedComponent, EntityTimerEvent>(OnLinkTimer);
    }

    private void OnConnectedInit(Entity<NodeScannerConnectedComponent> ent, ref ComponentInit args)
    {
        Schedule(ent);
    }

    private void OnConnectedHandleState(Entity<NodeScannerConnectedComponent> ent, ref ComponentHandleState args)
    {
        Schedule(ent);
    }

    private void OnLinkTimer(Entity<NodeScannerConnectedComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != LinkTimer ||
            !TryComp<NodeScannerComponent>(ent, out var scanner) ||
            !TryComp<TransformComponent>(ent, out var transform) ||
            !TryComp<TransformComponent>(ent.Comp.AttachedTo, out var artifactTransform))
            return;

        if (!_transform.InRange(artifactTransform.Coordinates, transform.Coordinates, scanner.MaxLinkedRange))
        {
            RemCompDeferred<NodeScannerConnectedComponent>(ent);
            return;
        }

        ent.Comp.NextUpdate = args.NextDeadline ?? args.FiredAt + ent.Comp.LinkUpdateInterval;
        Dirty(ent);
    }

    private void Schedule(Entity<NodeScannerConnectedComponent> ent)
    {
        _timers.SetTimerAt(ent, LinkTimer, ent.Comp.NextUpdate, ent.Comp.LinkUpdateInterval);
    }

    private void OnBeforeRangedInteract(EntityUid uid, NodeScannerComponent component, BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target || !HasComp<XenoArtifactComponent>(target))
            return;

        Entity<XenoArtifactUnlockingComponent?> unlockingEnt = TryComp<XenoArtifactUnlockingComponent>(target, out var unlockingComponent)
            ? (target, unlockingComponent)
            : (target, null);

        Attach((uid, component), unlockingEnt, args.User);

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
            Act = () => Attach((uid, component), (args.Target, unlockingComponent), args.User),
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
        if (!_timing.IsFirstTimePredicted)
            return;

        if (TryComp(device, out UseDelayComponent? useDelay)
            && !_useDelay.TryResetDelay((device, useDelay), true))
            return;

        var connected = EnsureComp<NodeScannerConnectedComponent>(device);
        EntityUid artifact = unlockingEnt;
        if (connected.AttachedTo != artifact)
        {
            connected.AttachedTo = artifact;
            Dirty(device, connected);
        }

        connected.NextUpdate = _timing.CurTime + connected.LinkUpdateInterval;
        Schedule((device.Owner, connected));

        _ui.TryOpenUi((device, null), NodeScannerUiKey.Key, actor, predicted: true);
    }
}
