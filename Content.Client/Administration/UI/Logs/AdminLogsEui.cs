using System.Linq;
using Content.Client.Eui;
using Content.Shared.Administration.Logs;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Shared.Administration.Logs.AdminLogsEuiMsg;

namespace Content.Client.Administration.UI.Logs;

[UsedImplicitly]
public sealed class AdminLogsEui : BaseEui
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public AdminLogsEui()
    {
        LogsWindow = new AdminLogsWindow();
        LogsWindow.OnClose += OnCloseWindow;
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
        SendMessage(new CloseEuiMessage());
    }

    private void OnCloseWindow()
    {
        if (ClydeWindow == null)
            SendMessage(new CloseEuiMessage());
    }

    private void RequestLogs()
    {
        var request = new LogsRequest(
            LogsControl.SelectedRoundId,
            LogsControl.Search,
            LogsControl.SelectedTypes.ToHashSet(),
            null,
            null,
            null,
            LogsControl.SelectedPlayers.Count != 0,
            LogsControl.SelectedPlayers.ToArray(),
            null,
            LogsControl.IncludeNonPlayerLogs,
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

        var monitor = _clyde.EnumerateMonitors().First();

        ClydeWindow = _clyde.CreateWindow(new WindowCreateParameters
        {
            Maximized = false,
            Title = "Admin Logs",
            Monitor = monitor,
            Width = 1000,
            Height = 400
        });

        LogsControl.Orphan();
        LogsWindow.Dispose();
        LogsWindow = null;

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
            case NewLogs newLogs:
                if (newLogs.Replace)
                {
                    LogsControl.SetLogs(newLogs.Logs);
                }
                else
                {
                    LogsControl.AddLogs(newLogs.Logs);
                }

                LogsControl.NextButton.Disabled = !newLogs.HasNext;
                break;

            case SetLogFilter setLogFilter:
                if (setLogFilter.Search != null)
                    LogsControl.LogSearch.SetText(setLogFilter.Search);

                if (setLogFilter.Types != null)
                    LogsControl.SetTypesSelection(setLogFilter.Types, setLogFilter.InvertTypes);

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
