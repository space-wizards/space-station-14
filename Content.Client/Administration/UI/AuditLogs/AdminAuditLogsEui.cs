using System.IO;
using System.Linq;
using Content.Client.Eui;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Shared.Administration.Logs.AdminAuditLogsEuiMsg;

namespace Content.Client.Administration.UI.AuditLogs;

[UsedImplicitly]
public sealed class AdminAuditLogsEui : BaseEui
{
    [Dependency] private readonly IFileDialogManager _dialogManager = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private const char CsvSeparator = ',';
    private const string CsvQuote = "\"";
    private const string CsvHeader = "Date,ID,AdminName,Action,Severity,Target,Message";

    private ISawmill _sawmill = default!;

    private AdminAuditLogsWindow? _window;
    private readonly AdminAuditLogsControl _control;

    private IClydeWindow? ClydeWindow;
    private WindowRoot? Root;

    private bool _firstState = true;
    private bool _suppressRoundChangeRequest;
    private bool _currentlyExportingLogs;

    public AdminAuditLogsEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _log.GetSawmill("admin.audit_logs.ui");

        _window = new AdminAuditLogsWindow();
        _control = _window.AuditLogs;

        _window.OnClose += OnCloseWindow;

        _control.SearchButton.OnPressed += _ => RequestLogs();
        _control.SearchLineEdit.OnTextEntered += _ => RequestLogs();
        _control.NextButton.OnPressed += _ => NextLogs();
        _control.ActionFiltersChanged += RequestLogs;
        _control.RoutineButton.OnPressed += _ => RequestLogs();
        _control.NotableButton.OnPressed += _ => RequestLogs();
        _control.CriticalButton.OnPressed += _ => RequestLogs();
        _control.ExportLogs.OnPressed += _ => ExportLogs();
        _control.PopOutButton.OnPressed += _ => PopOut();
        _control.RoundSpinBox.ValueChanged += _ =>
        {
            if (!_suppressRoundChangeRequest)
                RequestLogs();
        };
    }

    private void OnCloseWindow()
    {
        SendMessage(new CloseEuiMessage());
    }

    public override void HandleState(EuiStateBase state)
    {
        var s = (AdminAuditLogsEuiState) state;

        if (s.IsLoading)
            return;

        _control.SetMaxRound(s.MaxRoundId);
        _control.SetServerName(s.CurrentServerName);
        _control.UpdateCount(s.TotalLogs);

        if (!_firstState)
            return;

        _firstState = false;
        _suppressRoundChangeRequest = true;
        _control.RoundSpinBox.Value = s.MaxRoundId;
        _suppressRoundChangeRequest = false;

        RequestLogs();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not NewAuditLogs newLogs)
            return;

        if (newLogs.Replace)
            _control.SetLogs(newLogs.Logs);
        else
            _control.AddLogs(newLogs.Logs);

        _control.NextButton.Disabled = !newLogs.HasNext;
    }

    private void RequestLogs()
    {
        _control.NextButton.Disabled = true;

        SendMessage(new AuditLogsRequest
        {
            RoundId = _control.SelectedRoundId > 0 ? _control.SelectedRoundId : null,
            Search = _control.Search,
            Actions = new HashSet<AdminAuditAction>(_control.SelectedActions),
            Severities = new HashSet<AuditSeverity>(_control.SelectedSeverities),
            DateOrder = DateOrder.Descending
        });
    }

    private void NextLogs()
    {
        _control.NextButton.Disabled = true;
        SendMessage(new NextAuditLogsRequest());
    }

    private async void ExportLogs()
    {
        if (_currentlyExportingLogs)
            return;

        _currentlyExportingLogs = true;
        _control.ExportLogs.Disabled = true;

        var file = await _dialogManager.SaveFile(new FileDialogFilters(new FileDialogFilters.Group("csv")));
        if (file == null)
        {
            _currentlyExportingLogs = false;
            _control.ExportLogs.Disabled = false;
            return;
        }

        try
        {
            await using var writer = new StreamWriter(file.Value.fileStream, bufferSize: 4096);
            await writer.WriteLineAsync(CsvHeader);

            foreach (var log in _control.LoadedLogs)
            {
                await writer.WriteAsync(log.OccurredAt.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
                await writer.WriteAsync(CsvSeparator);
                await writer.WriteAsync(log.Id.ToString());
                await writer.WriteAsync(CsvSeparator);
                await writer.WriteAsync(CsvQuote);
                await writer.WriteAsync(log.AdminUserName.Replace(CsvQuote, CsvQuote + CsvQuote));
                await writer.WriteAsync(CsvQuote);
                await writer.WriteAsync(CsvSeparator);
                await writer.WriteAsync(log.Action.ToString());
                await writer.WriteAsync(CsvSeparator);
                await writer.WriteAsync(log.Severity.ToString());
                await writer.WriteAsync(CsvSeparator);
                var target = log.TargetPlayerUserName ?? log.TargetEntityName ?? "";
                await writer.WriteAsync(CsvQuote);
                await writer.WriteAsync(target.Replace(CsvQuote, CsvQuote + CsvQuote));
                await writer.WriteAsync(CsvQuote);
                await writer.WriteAsync(CsvSeparator);
                await writer.WriteAsync(CsvQuote);
                await writer.WriteAsync(log.Message.Replace(CsvQuote, CsvQuote + CsvQuote));
                await writer.WriteAsync(CsvQuote);
                await writer.WriteLineAsync();
            }
        }
        catch (Exception exc)
        {
            _sawmill.Error($"Error exporting audit logs:\n{exc.StackTrace}");
        }
        finally
        {
            await file.Value.fileStream.DisposeAsync();
            _currentlyExportingLogs = false;
            _control.ExportLogs.Disabled = false;
        }
    }

    private void PopOut()
    {
        if (_window == null)
            return;

        var monitor = _clyde.EnumerateMonitors().First();

        ClydeWindow = _clyde.CreateWindow(new WindowCreateParameters
        {
            Maximized = false,
            Title = Loc.GetString("admin-audit-logs-title"),
            Monitor = monitor,
            Width = 1100,
            Height = 400
        });

        _control.Orphan();
        _window.Dispose();
        _window = null;

        ClydeWindow.RequestClosed += OnRequestClosed;
        ClydeWindow.DisposeOnClose = true;

        Root = _uiManager.CreateWindowRoot(ClydeWindow);
        Root.AddChild(_control);
    }

    private void OnRequestClosed(WindowRequestClosedEventArgs args)
    {
        OnCloseWindow();
    }

    public override void Opened()
    {
        base.Opened();
        _window?.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _control.Dispose();
        _window?.Dispose();
        Root?.Dispose();
        ClydeWindow?.Dispose();
    }
}
