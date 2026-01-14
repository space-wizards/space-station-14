using System.IO;
using System.Linq;
using Content.Client.Administration.UI.CustomControls;
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
    [Dependency] private readonly IFileDialogManager _dialogManager = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private const char CsvSeparator = ',';
    private const string CsvQuote = "\"";
    private const string CsvHeader = "Date,ID,PlayerID,Severity,Type,Message";

    private ISawmill _sawmill;

    private bool _currentlyExportingLogs = false;

    public AdminLogsEui()
    {
        LogsWindow = new AdminLogsWindow();
        LogsWindow.OnClose += OnCloseWindow;
        LogsControl = LogsWindow.Logs;

        LogsControl.LogSearch.OnTextEntered += _ => RequestLogs();
        LogsControl.RefreshButton.OnPressed += _ => RequestLogs();
        LogsControl.NextButton.OnPressed += _ => NextLogs();
        LogsControl.PopOutButton.OnPressed += _ => PopOut();
        LogsControl.ExportLogs.OnPressed += _ => ExportLogs();

        _sawmill = _log.GetSawmill("admin.logs.ui");
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
            DateOrder.Descending);

        SendMessage(request);
    }

    private void NextLogs()
    {
        LogsControl.NextButton.Disabled = true;
        var request = new NextLogsRequest();
        SendMessage(request);
    }

    private async void ExportLogs()
    {
        if (_currentlyExportingLogs)
            return;

        _currentlyExportingLogs = true;
        LogsControl.ExportLogs.Disabled = true;

        var file = await _dialogManager.SaveFile(new FileDialogFilters(new FileDialogFilters.Group("csv")));

        if (file == null)
            return;

        try
        {
            // Buffer is set to 4KB for performance reasons. As the average export of 1000 logs is ~200KB
            await using var writer = new StreamWriter(file.Value.fileStream, bufferSize: 4096);
            await writer.WriteLineAsync(CsvHeader);
            foreach (var child in LogsControl.LogsContainer.Children)
            {
                if (child is not AdminLogLabel logLabel || !child.Visible)
                    continue;

                var log = logLabel.Log;

                // Date
                // I swear to god if someone adds ,s or "s to the other fields...
                await writer.WriteAsync(log.Date.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
                await writer.WriteAsync(CsvSeparator);
                // ID
                await writer.WriteAsync(log.Id.ToString());
                await writer.WriteAsync(CsvSeparator);
                // PlayerID
                var players = log.Players;
                for (var i = 0; i < players.Length; i++)
                {
                    await writer.WriteAsync(players[i] + (i == players.Length - 1 ? "" : " "));
                }
                await writer.WriteAsync(CsvSeparator);
                // Severity
                await writer.WriteAsync(log.Impact.ToString());
                await writer.WriteAsync(CsvSeparator);
                // Type
                await writer.WriteAsync(log.Type.ToString());
                await writer.WriteAsync(CsvSeparator);
                // Message
                await writer.WriteAsync(CsvQuote);
                await writer.WriteAsync(log.Message.Replace(CsvQuote, CsvQuote + CsvQuote));
                await writer.WriteAsync(CsvQuote);

                await writer.WriteLineAsync();
            }
        }
        catch (Exception exc)
        {
            _sawmill.Error($"Error when exporting admin log:\n{exc.StackTrace}");
        }
        finally
        {
            await file.Value.fileStream.DisposeAsync();
            _currentlyExportingLogs = false;
            LogsControl.ExportLogs.Disabled = false;
        }
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
            Title = Loc.GetString("admin-logs-title"),
            Monitor = monitor,
            Width = 1100,
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
        LogsControl.UpdateCount(round: s.RoundLogs);

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
