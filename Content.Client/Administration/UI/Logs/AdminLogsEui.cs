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
        LogsWindow = new AdminLogsWindow();
        LogsControl = LogsWindow.Logs;

        LogsControl.LogSearch.OnTextEntered += _ => RequestLogs();
        LogsControl.RefreshButton.OnPressed += _ => RequestLogs();
        LogsControl.NextButton.OnPressed += _ => NextLogs();
        LogsControl.PopOutButton.OnPressed += _ => PopOut();
    }

    private WindowRoot? Root { get; set; }

    private IClydeWindow? ClydeWindow { get; set; }

    private AdminLogsWindow? LogsWindow { get; set; }

    private AdminLogsControl LogsControl { get; }

    private bool FirstState { get; set; } = true;

    private void OnRequestClosed(WindowRequestClosedEventArgs args)
    {
        SendMessage(new Close());
    }

    private void RequestLogs()
    {
        var request = new LogsRequest(
            LogsControl.SelectedRoundId,
            LogsControl.SelectedTypes.ToHashSet(),
            null,
            null,
            null,
            LogsControl.SelectedPlayers.ToArray(),
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

    private void PopOut()
    {
        if (LogsWindow == null)
        {
            return;
        }

        LogsControl.Orphan();
        LogsWindow.Dispose();
        LogsWindow = null;

        var monitor = _clyde.EnumerateMonitors().First();

        ClydeWindow = _clyde.CreateWindow(new WindowCreateParameters
        {
            Maximized = false,
            Title = "Admin Logs",
            Monitor = monitor,
            Width = 1000,
            Height = 400
        });

        ClydeWindow.RequestClosed += OnRequestClosed;
        ClydeWindow.DisposeOnClose = true;

        Root = _uiManager.CreateWindowRoot(ClydeWindow);
        Root.AddChild(LogsControl);

        LogsControl.PopOutButton.Disabled = true;
        LogsControl.PopOutButton.Visible = false;
    }

    public override void HandleState(EuiStateBase state)
    {
        var s = (AdminLogsEuiState) state;

        if (s.IsLoading)
        {
            return;
        }

        LogsControl.SetCurrentRound(s.RoundId);
        LogsControl.SetPlayers(s.Players);

        if (!FirstState)
        {
            return;
        }

        FirstState = false;
        LogsControl.SetRoundSpinBox(s.RoundId);
        RequestLogs();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case NewLogs {Replace: true} newLogs:
                LogsControl.SetLogs(newLogs.Logs);
                break;
            case NewLogs {Replace: false} newLogs:
                LogsControl.AddLogs(newLogs.Logs);
                break;
        }
    }

    public override void Opened()
    {
        base.Opened();

        LogsWindow?.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        if (ClydeWindow != null)
        {
            ClydeWindow.RequestClosed -= OnRequestClosed;
        }

        LogsControl.Dispose();
        LogsWindow?.Dispose();
        Root?.Dispose();
        ClydeWindow?.Dispose();
    }
}
