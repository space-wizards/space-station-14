using Content.Shared.Anomaly;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Anomaly.Ui;

[UsedImplicitly]
public sealed class AnomalyScannerBoundUserInterface : BoundUserInterface
{
    private AnomalyScannerMenu? _menu;

    public AnomalyScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _menu = new AnomalyScannerMenu();
        _menu.OpenCentered();
        _menu.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AnomalyScannerUserInterfaceState msg)
            return;

        if (_menu == null)
            return;

        _menu.LastMessage = msg.Message;
        _menu.NextPulseTime = msg.NextPulseTime;
        _menu.UpdateMenu();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}

