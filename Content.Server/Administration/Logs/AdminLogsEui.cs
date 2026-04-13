using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Eui;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Administration.Logs.AdminLogsEuiMsg;

namespace Content.Server.Administration.Logs;

public sealed class AdminLogsEui : BaseEui
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IEntityManager _e = default!;
    [Dependency] private readonly ServerDbEntryManager _serverDbEntry = default!;

    private readonly ISawmill _sawmill;

    private int _clientBatchSize;
    private bool _isLoading = true;
    private readonly Dictionary<Guid, string> _players = new();
    private readonly Dictionary<int, string> _servers = new();
    private string _currentServerName = "";
    private int _roundLogs;
    private CancellationTokenSource _logSendCancellation = new();
    private LogFilter _filter;

    private readonly DefaultObjectPool<List<SharedAdminLog>> _adminLogListPool =
        new(new ListPolicy<SharedAdminLog>());

    public AdminLogsEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _logManager.GetSawmill(AdminLogManager.SawmillId);

        _configuration.OnValueChanged(CCVars.AdminLogsClientBatchSize, ClientBatchSizeChanged, true);

        _filter = new LogFilter
        {
            CancellationToken = _logSendCancellation.Token,
            Limit = _clientBatchSize
        };
    }

    private int CurrentRoundId => _e.System<GameTicker>().RoundId;

    private bool IsPreRound => CurrentRoundId <= 0;

    public override async void Opened()
    {
        base.Opened();

        _adminManager.OnPermsChanged += OnPermsChanged;

        // Resolve our own server ID and name so the initial filter is scoped
        // correctly before the client sends its first explicit LogsRequest.
        var serverEntity = await _serverDbEntry.ServerEntity;
        if (_filter.ServerId == null)
            _filter.ServerId = serverEntity.Id;
        _currentServerName = serverEntity.Name;

        if (IsPreRound)
        {
            // No round exists yet — show server-scoped recent logs without a round filter.
            _roundLogs = 0;
            _isLoading = false;
            StateDirty();
            return;
        }

        var roundId = _filter.Round ?? CurrentRoundId;
        await LoadFromDb(roundId);
    }

    private void ClientBatchSizeChanged(int value)
    {
        _clientBatchSize = value;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
        {
            Close();
        }
    }

    public override EuiStateBase GetNewState()
    {
        if (_isLoading)
        {
            return new AdminLogsEuiState(CurrentRoundId, new Dictionary<Guid, string>(), 0,
                new Dictionary<int, string>(), _currentServerName)
            {
                IsLoading = true
            };
        }

        return new AdminLogsEuiState(CurrentRoundId, _players, _roundLogs, _servers, _currentServerName);
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
        {
            return;
        }

        switch (msg)
        {
            case LogsRequest request:
            {
                _sawmill.Info($"Admin log request from admin with id {Player.UserId.UserId} and name {Player.Name}");

                _logSendCancellation.Cancel();
                _logSendCancellation = new CancellationTokenSource();

                var roundId = request.RoundId ?? CurrentRoundId;
                int? resolvedServerId;

                if (roundId <= 0)
                {
                    // Pre-round lobby — no round exists. Query by server only.
                    var serverEntity = await _serverDbEntry.ServerEntity;
                    resolvedServerId = serverEntity.Id;
                }
                else
                {
                    try
                    {
                        resolvedServerId = await LoadFromDb(roundId);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        _sawmill.Error($"Failed to load admin logs for round {roundId}: {e}");
                        return;
                    }
                }

                _filter = new LogFilter
                {
                    CancellationToken = _logSendCancellation.Token,
                    ServerId = resolvedServerId,
                    Round = roundId > 0 ? roundId : null,
                    Search = request.Search,
                    SearchMode = request.SearchMode,
                    Types = request.Types,
                    Impacts = request.Impacts,
                    Before = request.Before,
                    After = request.After,
                    IncludePlayers = request.IncludePlayers,
                    AnyPlayers = request.AnyPlayers,
                    AllPlayers = request.AllPlayers,
                    IncludeNonPlayers = request.IncludeNonPlayers,
                    DateOrder = request.DateOrder,
                    AnyEntities = request.AnyEntities,
                    LastLogId = null,
                    Limit = _clientBatchSize
                };

                SendLogs(true);
                break;
            }
            case NextLogsRequest:
            {
                _sawmill.Info($"Admin log next batch request from admin with id {Player.UserId.UserId} and name {Player.Name}");

                SendLogs(false);
                break;
            }
        }
    }

    public void SetLogFilter(string? search = null, bool invertTypes = false, HashSet<LogType>? types = null)
    {
        var message = new SetLogFilter(
            search,
            invertTypes,
            types);

        SendMessage(message);
    }

    private async void SendLogs(bool replace)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        List<SharedAdminLog> logs;
        try
        {
            logs = await Task.Run(async () => await _adminLogger.All(_filter, _adminLogListPool.Get),
                _filter.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to query admin logs: {e.Message}");
            SendMessage(new NewLogs(new List<SharedAdminLog>(), replace, false));
            return;
        }

        if (logs.Count > 0)
        {
            _filter.LogsSent += logs.Count;

            var cursorIndex = _filter.DateOrder switch
            {
                DateOrder.Ascending => 0,
                DateOrder.Descending => ^1,
                _ => throw new ArgumentOutOfRangeException(nameof(_filter.DateOrder), _filter.DateOrder, null)
            };

            var cursorLog = logs[cursorIndex];
            _filter.LastLogId = cursorLog.Id;
            _filter.LastOccurredAt = cursorLog.Date;
        }

        var message = new NewLogs(logs, replace, logs.Count >= _filter.Limit);

        SendMessage(message);

        _sawmill.Info($"Sent {logs.Count} logs to {Player.Name} in {stopwatch.Elapsed.TotalMilliseconds} ms");

        _adminLogListPool.Return(logs);
    }

    public override void Closed()
    {
        base.Closed();

        _configuration.UnsubValueChanged(CCVars.AdminLogsClientBatchSize, ClientBatchSizeChanged);
        _adminManager.OnPermsChanged -= OnPermsChanged;

        _logSendCancellation.Cancel();
        _logSendCancellation.Dispose();
    }

    private async Task<int> LoadFromDb(int roundId)
    {
        _isLoading = true;
        StateDirty();

        var cancel = _logSendCancellation.Token;
        var resolvedRound = await _adminLogger.Round(roundId);
        var logCount = await _adminLogger.CountLogs(roundId, cancel: cancel);
        var serverEntity = await _serverDbEntry.ServerEntity;

        var players = resolvedRound.Players
            .ToDictionary(player => player.UserId, player => player.LastSeenUserName);

        _players.Clear();
        foreach (var (id, name) in players)
            _players.Add(id, name);

        _servers.Clear();
        _servers[serverEntity.Id] = serverEntity.Name;

        _currentServerName = serverEntity.Name;

        _roundLogs = logCount;

        _isLoading = false;
        StateDirty();

        return resolvedRound.ServerId;
    }
}
