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
    private AdminLogsWindow Window { get; }

    public AdminLogsEui()
    {
        Window = new AdminLogsWindow();
        Window.OnClose += () => SendMessage(new Close());
        Window.LogSearch.OnTextEntered += _ => RequestLogs();
        Window.RefreshButton.OnPressed += _ => RequestLogs();
        Window.NextButton.OnPressed += _ => NextLogs();
    }

    private void RequestLogs()
    {
        var search = Window.LogSearch.Text;
        var types = Window.GetSelectedLogTypes();
        var players = Window.GetSelectedPlayerIds();

        var request = new LogsRequest(
            search,
            types,
            null,
            null,
            null,
            players,
            null,
            DateOrder.Ascending);

        SendMessage(request);
    }

    private void NextLogs()
    {
        var request = new NextLogsRequest();
        SendMessage(request);
    }

    public override void Opened()
    {
        Window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        var s = (AdminLogsEuiState) state;

        if (s.IsLoading)
        {
            return;
        }

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
