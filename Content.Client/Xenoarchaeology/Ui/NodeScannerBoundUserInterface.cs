using Content.Shared.Xenoarchaeology.Equipment.Components;

namespace Content.Client.Xenoarchaeology.Ui;

public sealed class NodeScannerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private NodeScannerDisplay? _scannerDisplay;

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();

        _scannerDisplay = new NodeScannerDisplay(Owner);
        _scannerDisplay.OpenCentered();
        _scannerDisplay.OnClose += Close;
    }

    /// <summary>
    /// Update UI state based on corresponding component.
    /// </summary>
    public void Update(Entity<NodeScannerComponent> ent)
    {
        _scannerDisplay?.Update(ent);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _scannerDisplay?.Dispose();
    }
}
