using Content.Shared.Anomaly;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Anomaly.Ui;

[UsedImplicitly]
public sealed partial class AnomalyScannerBoundUserInterface : BoundUserInterface
{
    private static readonly EntityTimerId PulseTimer = new("pulse");
    private static readonly EntityTimerId RefreshTimer = new("refresh");

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
        if (msg.NextPulseTime is { } deadline)
        {
            SetTimerAt(PulseTimer, deadline);
            SetTimer(RefreshTimer, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
        else
        {
            CancelTimer(PulseTimer);
            CancelTimer(RefreshTimer);
        }

        UpdateRemainingTime();
    }

    protected override void OnTimer(EntityTimerEvent timer)
    {
        if (timer.Id == PulseTimer)
            CancelTimer(RefreshTimer);

        if (timer.Id == PulseTimer || timer.Id == RefreshTimer)
            UpdateRemainingTime();
    }

    private void UpdateRemainingTime()
    {
        var remaining = TryGetTimer(PulseTimer, out var timer) ? timer.Remaining : (TimeSpan?) null;
        _menu?.UpdateMenu(remaining);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
