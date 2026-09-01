using Robust.Client.UserInterface;

namespace Content.Client.Xenoarchaeology.Ui;

/// <summary>
/// BUI for hand-held xeno artifact scanner.
/// Window updates based on component state, this only handles the lifecycle of the window.
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
}
