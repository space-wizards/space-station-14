using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary> Controls behaviour of artifact node scanner device. </summary>
public sealed class NodeScannerSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NodeScannerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
        SubscribeLocalEvent<NodeScannerComponent, GetVerbsEvent<UtilityVerb>>(AddScanVerb);
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        var scannerQuery = EntityQueryEnumerator<NodeScannerComponent, TransformComponent>();
        while (scannerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            if(component.AttachedTo == null)
                continue;

            if (component.NextUpdate > _timing.CurTime)
                continue;

            component.NextUpdate = _timing.CurTime + component.LinkUpdateInterval;

            var attachedArtifact = EntityManager.GetEntity(component.AttachedTo);
            if(!attachedArtifact.HasValue)
                return;

            var artifactCoordinates = Transform(attachedArtifact.Value).Coordinates;

            if (!_transform.InRange(artifactCoordinates, transform.Coordinates, component.MaxLinkedRange))
            {
                //scanner is too far, disconnect
                component.AttachedTo = null;
                Dirty(uid, component);
            }
        }
    }

    private void OnBeforeRangedInteract(EntityUid uid, NodeScannerComponent component, BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
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

        var artifactNetEntity = EntityManager.GetNetEntity(unlockingEnt.Owner);
        if (device.Comp.AttachedTo != artifactNetEntity)
        {
            device.Comp.AttachedTo = artifactNetEntity;
            Dirty(device);
        }

        _ui.TryOpenUi((device, null), NodeScannerUiKey.Key, actor, predicted: true);
    }
}
