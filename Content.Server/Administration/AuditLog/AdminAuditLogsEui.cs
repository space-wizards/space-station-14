using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Eui;
using Robust.Shared.Configuration;
using static Content.Shared.Administration.Logs.AdminAuditLogsEuiMsg;

namespace Content.Server.Administration.AuditLog;

public sealed class AdminAuditLogsEui : BaseEui
{
    [Dependency] private readonly IAdminAuditLogManager _auditLogManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ServerDbEntryManager _serverDbEntry = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly ISawmill _sawmill;
    private CancellationTokenSource _queryCancellation = new();
    private AuditLogFilter? _currentFilter;
    private int _serverId;
    private int _currentRoundId;
    private int _totalLogs;
    private bool _isLoading = true;
    private int _clientBatchSize;

    public AdminAuditLogsEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("admin.audit_logs.ui");
        _configuration.OnValueChanged(CCVars.AdminLogsClientBatchSize, ClientBatchSizeChanged, true);

        _ = _auditLogManager;
        _ = _entityManager;
    }

    private int CurrentRoundId => _entityManager.System<GameTicker>().RoundId;

    public override async void Opened()
    {
        base.Opened();

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
        {
            Close();
            return;
        }

        _adminManager.OnPermsChanged += OnPermsChanged;

        await LoadInitialData(CurrentRoundId, _queryCancellation.Token);
    }

    public override EuiStateBase GetNewState()
    {
        if (_isLoading)
        {
            return new AdminAuditLogsEuiState(CurrentRoundId, CurrentRoundId, 0, string.Empty)
            {
                IsLoading = true
            };
        }

        return new AdminAuditLogsEuiState(_currentRoundId, CurrentRoundId, _totalLogs, _currentServerName)
        {
            IsLoading = false
        };
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
        {
            Close();
            return;
        }

        switch (msg)
        {
            case AuditLogsRequest request:
                _sawmill.Info($"Audit log request from admin {Player.Name} ({Player.UserId.UserId})");
                StartNewQuery();

                try
                {
                    var roundId = request.RoundId ?? CurrentRoundId;
                    await LoadInitialData(roundId, _queryCancellation.Token);

                    _currentFilter = new AuditLogFilter
                    {
                        CancellationToken = _queryCancellation.Token,
                        Round = roundId,
                        ServerId = _serverId,
                        Search = request.Search,
                        SearchMode = request.SearchMode,
                        Actions = request.Actions,
                        Severities = request.Severities,
                        AdminUserId = request.AdminUserId,
                        TargetPlayerUserId = request.TargetPlayerUserId,
                        DateOrder = request.DateOrder,
                        Limit = _clientBatchSize,
                        LastLogId = null,
                        LastOccurredAt = null
                    };

                    await SendLogs(true);
                }
                catch (OperationCanceledException)
                {
                }

                break;

            case NextAuditLogsRequest:
                if (_currentFilter == null)
                    return;

                _sawmill.Info($"Audit log next request from admin {Player.Name} ({Player.UserId.UserId})");

                try
                {
                    await SendLogs(false);
                }
                catch (OperationCanceledException)
                {
                }

                break;
        }
    }

    private string _currentServerName = string.Empty;

    private async Task LoadInitialData(int roundId, CancellationToken cancel)
    {
        _isLoading = true;
        StateDirty();

        var server = await _serverDbEntry.ServerEntity;
        _serverId = server.Id;
        _currentServerName = server.Name;
        _currentRoundId = roundId;

        var countFilter = new AuditLogFilter
        {
            CancellationToken = cancel,
            Round = roundId,
            ServerId = _serverId
        };

        _totalLogs = await _db.CountAuditLogs(countFilter);

        _isLoading = false;
        StateDirty();
    }

    private async Task SendLogs(bool replace)
    {
        if (_currentFilter == null)
            return;

        var logs = await _db.GetAuditLogs(_currentFilter);

        if (logs.Count > 0)
        {
            var cursorIndex = _currentFilter.DateOrder == DateOrder.Ascending ? 0 : logs.Count - 1;
            var cursorLog = logs[cursorIndex];

            _currentFilter.LastLogId = cursorLog.Id;
            _currentFilter.LastOccurredAt = cursorLog.OccurredAt;
        }

        var hasNext = logs.Count >= (_currentFilter.Limit ?? logs.Count);

        SendMessage(new NewAuditLogs
        {
            Logs = logs,
            Replace = replace,
            HasNext = hasNext
        });
    }

    private void StartNewQuery()
    {
        _queryCancellation.Cancel();
        _queryCancellation.Dispose();
        _queryCancellation = new CancellationTokenSource();
    }

    private void ClientBatchSizeChanged(int value)
    {
        _clientBatchSize = value;

        if (_currentFilter != null)
            _currentFilter.Limit = value;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
        {
            Close();
        }
    }

    public override void Closed()
    {
        base.Closed();

        _configuration.UnsubValueChanged(CCVars.AdminLogsClientBatchSize, ClientBatchSizeChanged);
        _adminManager.OnPermsChanged -= OnPermsChanged;

        _queryCancellation.Cancel();
        _queryCancellation.Dispose();
    }
}
