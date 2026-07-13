using Content.Client.Eui;
using Content.Shared.Administration.AdminEventLog;
using Content.Shared.Eui;

namespace Content.Client.Administration.UI.Tabs.AdminTab;

public sealed class AdminEventLogEui : BaseEui
{
    private AdminEventLogWindow EventLogWindow { get; }

    public AdminEventLogEui()
    {
        EventLogWindow = new AdminEventLogWindow();
    }

    public override void HandleState(EuiStateBase state)
    {
        var s = (AdminEventLogEuiState)state;

        EventLogWindow.SetCurrentRound(s.RoundId);
    }
}
