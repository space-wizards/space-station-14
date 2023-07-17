// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Paper;
using Content.Client.SS220.ButtScan.UI;
using Content.Shared.Paper;
using Content.Shared.SS220.ButtScan;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.ButtScan;

public sealed class ButtScanBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityMgr = default!;

    private ButtScanWindow? _window;
    private readonly EntityUid _paperEntity;

    public ButtScanBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _paperEntity = owner;
    }

    /// <inheritdoc/>
    protected override void Open()
    {
        _window = new ButtScanWindow();
        _window.OnClose += Close;

        if (_entityMgr.TryGetComponent<ButtScanComponent>(_paperEntity, out var scan) &&
            _entityMgr.TryGetComponent<PaperVisualsComponent>(_paperEntity, out var paperVisuals))
            _window.InitVisuals(scan, paperVisuals);

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window?.Populate((SharedPaperComponent.PaperBoundUserInterfaceState) state);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if(disposing)
            _window?.Dispose();
    }
}
