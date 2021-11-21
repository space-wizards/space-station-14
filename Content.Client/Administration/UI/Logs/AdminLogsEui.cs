using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Eui;
using JetBrains.Annotations;
using static Content.Shared.Administration.AdminLogsEuiMsg;

namespace Content.Client.Administration.UI.Logs;

[UsedImplicitly]
public class AdminLogsEui : BaseEui
{
    public AdminLogsEui()
    {
        Window = new AdminLogsWindow();
        Window.OnClose += () => SendMessage(new Close());
        Window.LogSearch.OnTextEntered += _ => RequestLogs();
        Window.RefreshButton.OnPressed += _ => RequestLogs();
        Window.NextButton.OnPressed += _ => NextLogs();
    }

    private AdminLogsWindow Window { get; }

    private bool FirstState { get; set; } = true;

    private void RequestLogs()
    {
        var round = Window.GetSelectedRoundId();
        var types = Window.GetSelectedLogTypes();
        var players = Window.GetSelectedPlayerIds();

        var request = new LogsRequest(
            round,
            types,
            null,
            null,
            players,
            null,
            null,
            DateOrder.Descending);

        SendMessage(request);
    }

    private void NextLogs()
    {
        var request = new NextLogsRequest();
        SendMessage(request);
    }

    private void TrySetFirstState(AdminLogsEuiState state)
    {
        if (!FirstState)
        {
            return;
        }

        FirstState = false;
        Window.SetCurrentRound(state.RoundId);
        Window.SetRoundSpinBox(state.RoundId);
    }

    public override void Opened()
    {
        Window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        var s = (AdminLogsEuiState) state;

        TrySetFirstState(s);

        if (s.IsLoading)
        {
            return;
        }

        Window.SetCurrentRound(s.RoundId);
        Window.SetPlayers(s.Players);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case NewLogs {Replace: true} newLogs:
                Window.SetLogs(newLogs.Logs);
                break;
            case NewLogs {Replace: false} newLogs:
                Window.AddLogs(newLogs.Logs);
                break;
        }
    }

    public override void Closed()
    {
        base.Closed();

        Window.Close();
        Window.Dispose();
    }
}
