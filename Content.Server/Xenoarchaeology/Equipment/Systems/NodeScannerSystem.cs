using Content.Server.Popups;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Robust.Shared.Player;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

public sealed class NodeScannerSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NodeScannerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, NodeScannerComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (!TryComp<ArtifactComponent>(args.Target, out var artifact) || artifact.CurrentNode == null)
            return;

        if (args.Handled)
            return;
        args.Handled = true;

        var target = args.Target.Value;
        _useDelay.BeginDelay(uid);
        _popupSystem.PopupEntity(Loc.GetString("node-scan-popup",
            ("id", $"{artifact.CurrentNode.Id}")), target);
    }
}
