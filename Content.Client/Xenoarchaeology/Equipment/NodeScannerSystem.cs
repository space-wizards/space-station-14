using Content.Client.Xenoarchaeology.Ui;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Xenoarchaeology.Equipment;

/// <inheritdoc cref="SharedNodeScannerSystem"/>
public sealed class NodeScannerSystem : SharedNodeScannerSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NodeScannerComponent, AfterAutoHandleStateEvent>(OnAnalysisConsoleAfterAutoHandleState);
    }

    protected override void TryOpenUi(Entity<NodeScannerComponent> device, EntityUid actor)
    {
        _ui.TryOpenUi(device.Owner, NodeScannerUiKey.Key, actor, true);
    }

    private void OnAnalysisConsoleAfterAutoHandleState(Entity<NodeScannerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<NodeScannerBoundUserInterface>(ent.Owner, NodeScannerUiKey.Key, out var bui))
            bui.Update(ent);
    }
}
