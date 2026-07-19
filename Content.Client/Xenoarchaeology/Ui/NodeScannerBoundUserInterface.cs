using Robust.Client.UserInterface;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Timing;

namespace Content.Client.Xenoarchaeology.Ui;

/// <summary>
/// BUI for hand-held xeno artifact scanner,  server-provided UI updates.
/// </summary>
public sealed class NodeScannerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly EntityTimerId RefreshTimer = new("refresh");

    [ViewVariables]
    private NodeScannerDisplay? _scannerDisplay;

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();

        _scannerDisplay = this.CreateWindow<NodeScannerDisplay>();
        _scannerDisplay.SetOwner(Owner);

        if (EntMan.TryGetComponent<NodeScannerComponent>(Owner, out var scanner))
            SetTimer(RefreshTimer, TimeSpan.Zero, scanner.DisplayDataUpdateInterval);
    }

    protected override void OnTimer(EntityTimerEvent timer)
    {
        if (timer.Id == RefreshTimer)
            _scannerDisplay?.Refresh(timer.FiredAt);
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
