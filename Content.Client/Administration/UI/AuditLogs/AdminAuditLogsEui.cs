using Content.Client.Eui;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Eui;
using JetBrains.Annotations;
using static Content.Shared.Administration.Logs.AdminAuditLogsEuiMsg;

namespace Content.Client.Administration.UI.AuditLogs;

[UsedImplicitly]
public sealed class AdminAuditLogsEui : BaseEui
{
    private readonly AdminAuditLogsWindow _window;
    private readonly AdminAuditLogsControl _control;

    private bool _firstState = true;
    private bool _suppressRoundChangeRequest;

    public AdminAuditLogsEui()
    {
        _window = new AdminAuditLogsWindow();
        _control = _window.AuditLogs;

        _window.OnClose += OnCloseWindow;

        _control.SearchButton.OnPressed += _ => RequestLogs();
        _control.SearchLineEdit.OnTextEntered += _ => RequestLogs();
        _control.NextButton.OnPressed += _ => NextLogs();
        _control.RoutineButton.OnPressed += _ => RequestLogs();
        _control.NotableButton.OnPressed += _ => RequestLogs();
        _control.CriticalButton.OnPressed += _ => RequestLogs();
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

        _control.SetCurrentRound(s.RoundId);
        _control.SetServerName(s.CurrentServerName);
        _control.UpdateCount(s.TotalLogs);

        if (!_firstState)
            return;

        _firstState = false;
        _suppressRoundChangeRequest = true;
        _control.RoundSpinBox.Value = s.RoundId;
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
            Severities = new HashSet<AuditSeverity>(_control.SelectedSeverities),
            DateOrder = DateOrder.Descending
        });
    }

    private void NextLogs()
    {
        _control.NextButton.Disabled = true;
        SendMessage(new NextAuditLogsRequest());
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _control.Dispose();
        _window.Dispose();
    }
}
