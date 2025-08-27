using Robust.Client.UserInterface;

namespace Content.Client.Xenoarchaeology.Ui;

/// <summary>
/// BUI for hand-held xeno artifact scanner,  server-provided UI updates.
/// </summary>
public sealed class NodeScannerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private NodeScannerDisplay? _scannerDisplay;

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();

        _scannerDisplay = this.CreateWindow<NodeScannerDisplay>();
        _scannerDisplay.SetOwner(Owner);
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
