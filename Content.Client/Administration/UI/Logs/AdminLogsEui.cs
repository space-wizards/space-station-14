using System.Linq;
using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using static Content.Shared.Administration.AdminLogsEuiMsg;

namespace Content.Client.Administration.UI.Logs;

[UsedImplicitly]
public class AdminLogsEui : BaseEui
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public AdminLogsEui()
    {
        var monitor = _clyde.EnumerateMonitors().First();

        ClydeWindow = _clyde.CreateWindow(new WindowCreateParameters
        {
            Maximized = true,
            Title = "Admin Logs",
            Monitor = monitor
        });

        ClydeWindow.RequestClosed += OnRequestClosed;
        ClydeWindow.DisposeOnClose = true;

        LogsWindow = new AdminLogsWindow();
        LogsWindow.LogSearch.OnTextEntered += _ => RequestLogs();
        LogsWindow.RefreshButton.OnPressed += _ => RequestLogs();
        LogsWindow.NextButton.OnPressed += _ => NextLogs();

        Root = _uiManager.CreateWindowRoot(ClydeWindow);
        Root.AddChild(LogsWindow);
    }

    private WindowRoot Root { get; }

    private IClydeWindow ClydeWindow { get; }

    private AdminLogsWindow LogsWindow { get; }

    private bool FirstState { get; set; } = true;

    private void OnRequestClosed(WindowRequestClosedEventArgs args)
    {
        SendMessage(new Close());
    }

    private void RequestLogs()
    {
        var request = new LogsRequest(
            LogsWindow.SelectedRoundId,
            LogsWindow.SelectedTypes.ToList(),
            null,
            null,
            null,
            LogsWindow.SelectedPlayers.ToArray(),
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

    private bool TrySetFirstState(AdminLogsEuiState state)
    {
        if (!FirstState)
        {
            return false;
        }

        FirstState = false;
        LogsWindow.SetCurrentRound(state.RoundId);
        LogsWindow.SetRoundSpinBox(state.RoundId);
        return true;
    }

    public override void HandleState(EuiStateBase state)
    {
        var s = (AdminLogsEuiState) state;

        var first = TrySetFirstState(s);

        if (s.IsLoading)
        {
            return;
        }

        LogsWindow.SetCurrentRound(s.RoundId);
        LogsWindow.SetPlayers(s.Players);

        if (first)
        {
            RequestLogs();
        }
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case NewLogs {Replace: true} newLogs:
                LogsWindow.SetLogs(newLogs.Logs);
                break;
            case NewLogs {Replace: false} newLogs:
                LogsWindow.AddLogs(newLogs.Logs);
                break;
        }
    }

    public override void Closed()
    {
        base.Closed();

        ClydeWindow.RequestClosed -= OnRequestClosed;

        LogsWindow.Dispose();
        Root.Dispose();
        ClydeWindow.Dispose();
    }
}
