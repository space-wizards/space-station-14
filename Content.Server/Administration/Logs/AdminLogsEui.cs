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

public sealed partial class AdminLogsEui : BaseEui
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IEntityManager _e = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ServerDbEntryManager _serverDbEntry = default!;

    private readonly ISawmill _sawmill;

    private int _clientBatchSize;
    private bool _isLoading = true;
    private readonly Dictionary<Guid, string> _players = new();
    private readonly Dictionary<int, string> _servers = new();
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

    public override async void Opened()
    {
        base.Opened();

        _adminManager.OnPermsChanged += OnPermsChanged;

        // Resolve our own server ID so the initial filter is scoped correctly
        // before the client sends its first explicit LogsRequest.
        if (_filter.ServerId == null)
            _filter.ServerId = (await _serverDbEntry.ServerEntity).Id;

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
                new Dictionary<int, string>())
            {
                IsLoading = true
            };
        }

        return new AdminLogsEuiState(CurrentRoundId, _players, _roundLogs, _servers);
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

                // Resolve the requested server ID.
                // null, use the current server.
                // Explicit ID, use that server.
                // The server-side always enforces a server scope so logs from unrelated
                // servers are never leaked.
                var serverId = request.ServerId ?? (await _serverDbEntry.ServerEntity).Id;

                _filter = new LogFilter
                {
                    CancellationToken = _logSendCancellation.Token,
                    ServerId = serverId,
                    Round = request.RoundId,
                    Search = request.Search,
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

                var roundId = _filter.Round ??= CurrentRoundId;
                await LoadFromDb(roundId);

                SendLogs(true, _filter);
                break;
            }
            case NextLogsRequest:
            {
                _sawmill.Info($"Admin log next batch request from admin with id {Player.UserId.UserId} and name {Player.Name}");

                SendLogs(false, _filter);
                break;
            }
        }
    }

    public void SetLogFilter(string? search = null, List<Guid>? players = null, bool invertTypes = false, HashSet<LogType>? types = null)
    {
        var message = new SetLogFilter(
            search,
            players,
            invertTypes,
            types);

        SendMessage(message);
    }

    private async void SendLogs(bool replace, LogFilter filter)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var logs = await Task.Run(async () => await _adminLogs.All(filter, _adminLogListPool.Get),
            filter.CancellationToken);

        if (logs.Count > 0)
        {
            filter.LogsSent += logs.Count;

            var cursorIndex = _filter.DateOrder switch
            {
                DateOrder.Ascending => 0,
                DateOrder.Descending => ^1,
                _ => throw new ArgumentOutOfRangeException(nameof(filter.DateOrder), filter.DateOrder, null)
            };

            var cursorLog = logs[cursorIndex];
            _filter.LastLogId = cursorLog.Id;
            _filter.LastOccurredAt = cursorLog.Date;
        }

        var message = new NewLogs(logs, replace, logs.Count >= filter.Limit);

        if (filter.CancellationToken.IsCancellationRequested)
            return;

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

    private async Task LoadFromDb(int roundId)
    {
        _isLoading = true;
        StateDirty();

        var round = _adminLogs.Round(roundId);
        var count = _adminLogs.CountLogs(roundId);
        var servers = _db.GetAllServers();
        await Task.WhenAll(round, count, servers);

        var players = (await round).Players
            .ToDictionary(player => player.UserId, player => player.LastSeenUserName);

        _players.Clear();
        foreach (var (id, name) in players)
            _players.Add(id, name);

        _servers.Clear();
        foreach (var server in await servers)
            _servers[server.Id] = server.Name;

        _roundLogs = await count;

        _isLoading = false;
        StateDirty();
    }
}
