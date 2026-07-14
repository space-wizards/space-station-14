using Content.Client.Eui;
using Content.Shared.Administration.AdminEventLog;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.Tabs.AdminTab;

[UsedImplicitly]
public sealed class AdminEventLogEui : BaseEui
{
    private AdminEventLogWindow? EventLogWindow { get; }

    public AdminEventLogEui()
    {
        EventLogWindow = new AdminEventLogWindow();
    }

    public override void Opened()
    {
        base.Opened();

        EventLogWindow?.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        EventLogWindow?.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        var s = (AdminEventLogEuiState)state;

        EventLogWindow?.SetCurrentRound(s.RoundId);
        EventLogWindow?.SetRoundSpinBox(s.RoundId);
    }
}
