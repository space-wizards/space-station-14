// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.ButtScan.UI;
using Content.Shared.SS220.ButtScan;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.ButtScan;

public sealed class ButtScanBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityMgr = default!;

    private ButtScanWindow? _window;
    private readonly EntityUid _paperEntity;

    public ButtScanBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _paperEntity = owner.Owner;
    }

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _window = new ButtScanWindow();
        _window.OnClose += Close;

        if (_entityMgr.TryGetComponent<ButtScanComponent>(_paperEntity, out var scan))
            _window.InitVisuals(scan);

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if(disposing)
            _window?.Dispose();
    }
}
