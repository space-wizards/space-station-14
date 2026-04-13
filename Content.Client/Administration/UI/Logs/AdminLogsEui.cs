using System.IO;
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
    [Dependency] private readonly IFileDialogManager _dialogManager = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private const char CsvSeparator = ',';
    private const string CsvQuote = "\"";
    private const string CsvHeader = "Date,ID,PlayerID,EntityParticipation,Severity,Type,Message";

    private ISawmill _sawmill;

    private bool _currentlyExportingLogs = false;

    public AdminLogsEui()
    {
        LogsWindow = new AdminLogsWindow();
        LogsWindow.OnClose += OnCloseWindow;
        LogsControl = LogsWindow.Logs;

        LogsControl.LogSearch.OnTextEntered += _ => RequestLogs();
        LogsControl.EntityUidSearch.OnTextEntered += _ => RequestLogs();
        LogsControl.SearchButton.OnPressed += _ => RequestLogs();
        LogsControl.NextButton.OnPressed += _ => NextLogs();
        LogsControl.PopOutButton.OnPressed += _ => PopOut();
        LogsControl.ExportLogs.OnPressed += _ => ExportLogs();

        // Auto-request logs when the round changes
        LogsControl.RoundSpinBox.ValueChanged += _ => RequestLogs();

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
        // Parse entity UID search: comma- or space-separated.
        int[]? anyEntities = null;
        var entityText = LogsControl.EntitySearch;
        if (!string.IsNullOrWhiteSpace(entityText))
        {
            anyEntities = entityText
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var uid) ? (int?) uid : null)
                .Where(uid => uid.HasValue)
                .Select(uid => uid!.Value)
                .ToArray();

            if (anyEntities.Length == 0)
                anyEntities = null;
        }

        var hasPlayerFilter = LogsControl.SelectedPlayers.Count != 0;
        var request = new LogsRequest(
            LogsControl.SelectedRoundId,
            LogsControl.Search,
            LogsControl.SelectedTypes.ToHashSet(),
            LogsControl.SelectedImpacts.ToHashSet(),
            null,
            null,
            true,
            hasPlayerFilter ? LogsControl.SelectedPlayers.ToArray() : null,
            null,
            LogsControl.IncludeNonPlayerLogs || !hasPlayerFilter,
            DateOrder.Descending,
            anyEntities: anyEntities,
            searchMode: LogsControl.SelectedSearchMode);

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
        {
            _currentlyExportingLogs = false;
            LogsControl.ExportLogs.Disabled = false;
            return;
        }

        try
        {
            // Buffer is set to 4KB for performance reasons. As the average export of 1000 logs is ~200KB
            await using var writer = new StreamWriter(file.Value.fileStream, bufferSize: 4096);
            await writer.WriteLineAsync(CsvHeader);
            foreach (var log in LogsControl.GetShownLogs())
            {
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
                // EntityParticipation
                var entities = log.Entities;
                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    await writer.WriteAsync($"{entity.EntityUid}:{entity.Role}:{entity.PrototypeId ?? entity.EntityName ?? string.Empty}" + (i == entities.Length - 1 ? "" : " "));
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
        LogsControl.SetServerName(s.CurrentServerName);
        LogsControl.UpdateCount(round: s.RoundLogs);

        if (!FirstState)
        {
            return;
        }

        FirstState = false;
        LogsControl.SetRoundSpinBox(s.RoundId);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case NewLogs newLogs:
                if (newLogs.Replace)
                {
                    LogsControl.ClearLogs();
                    LogsControl.AddLogs(newLogs.Logs);
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
