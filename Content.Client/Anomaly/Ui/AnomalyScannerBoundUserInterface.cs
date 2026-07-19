using Content.Shared.Anomaly;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Anomaly.Ui;

[UsedImplicitly]
public sealed partial class AnomalyScannerBoundUserInterface : BoundUserInterface
{
    private static readonly EntityTimerId RefreshTimer = new("refresh");

    [Dependency] private IGameTiming _timing = default!;
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
        _menu.UpdateMenu(_timing.CurTime);
        if (msg.NextPulseTime != null)
            SetTimer(RefreshTimer, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        else
            CancelTimer(RefreshTimer);
    }

    protected override void OnTimer(EntityTimerEvent timer)
    {
        if (timer.Id == RefreshTimer)
            _menu?.UpdateMenu(_timing.CurTime);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
