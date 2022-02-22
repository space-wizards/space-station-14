using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Eui;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using static Content.Shared.Administration.AdminLogsEuiMsg;

namespace Content.Server.Administration.Logs;

public sealed class AdminLogsEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private readonly ISawmill _sawmill;
    private readonly AdminLogSystem _logSystem;

    private int _clientBatchSize;
    private bool _isLoading = true;
    private readonly Dictionary<Guid, string> _players = new();
    private CancellationTokenSource _logSendCancellation = new();
    private LogFilter _filter;

    public AdminLogsEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _logManager.GetSawmill(AdminLogSystem.SawmillId);

        _configuration.OnValueChanged(CCVars.AdminLogsClientBatchSize, ClientBatchSizeChanged, true);

        _logSystem = EntitySystem.Get<AdminLogSystem>();

        _filter = new LogFilter
        {
            CancellationToken = _logSendCancellation.Token,
            Limit = _clientBatchSize
        };
    }

    public int CurrentRoundId => EntitySystem.Get<GameTicker>().RoundId;

    public override async void Opened()
    {
        base.Opened();

        _adminManager.OnPermsChanged += OnPermsChanged;

        var roundId = _filter.Round ?? EntitySystem.Get<GameTicker>().RoundId;
        LoadFromDb(roundId);
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
            return new AdminLogsEuiState(CurrentRoundId, new Dictionary<Guid, string>())
            {
                IsLoading = true
            };
        }

        var state = new AdminLogsEuiState(CurrentRoundId, _players);

        return state;
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
        {
            return;
        }

        switch (msg)
        {
            case Close _:
            {
                Close();
                break;
            }
            case LogsRequest request:
            {
                _sawmill.Info($"Admin log request from admin with id {Player.UserId.UserId} and name {Player.Name}");

                _logSendCancellation.Cancel();
                _logSendCancellation = new CancellationTokenSource();
                _filter = new LogFilter
                {
                    CancellationToken = _logSendCancellation.Token,
                    Round = request.RoundId,
                    Types = request.Types,
                    Impacts = request.Impacts,
                    Before = request.Before,
                    After = request.After,
                    AnyPlayers = request.AnyPlayers,
                    AllPlayers = request.AllPlayers,
                    LastLogId = 0,
                    Limit = _clientBatchSize
                };

                var roundId = _filter.Round ??= EntitySystem.Get<GameTicker>().RoundId;
                LoadFromDb(roundId);

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

    private async void SendLogs(bool replace)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // TODO ADMIN LOGS array pool
        List<SharedAdminLog> logs = default!;

        await Task.Run(async () =>
        {
            logs = await _logSystem.All(_filter);
        }, _filter.CancellationToken);

        if (logs.Count > 0)
        {
            _filter.LogsSent += logs.Count;

            var largestId = _filter.DateOrder switch
            {
                DateOrder.Ascending => ^1,
                DateOrder.Descending => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(_filter.DateOrder), _filter.DateOrder, null)
            };

            _filter.LastLogId = logs[largestId].Id;
        }

        var message = new NewLogs(logs, replace);

        SendMessage(message);

        _sawmill.Info($"Sent {logs.Count} logs to {Player.Name} in {stopwatch.Elapsed.TotalMilliseconds} ms");
    }

    public override void Closed()
    {
        base.Closed();

        _configuration.UnsubValueChanged(CCVars.AdminLogsClientBatchSize, ClientBatchSizeChanged);
        _adminManager.OnPermsChanged -= OnPermsChanged;

        _logSendCancellation.Cancel();
        _logSendCancellation.Dispose();
    }

    private async void LoadFromDb(int roundId)
    {
        _isLoading = true;
        StateDirty();

        var round = await Task.Run(() => _logSystem.Round(roundId));
        var players = round.Players
            .ToDictionary(player => player.UserId, player => player.LastSeenUserName);

        _players.Clear();

        foreach (var (id, name) in players)
        {
            _players.Add(id, name);
        }

        _isLoading = false;
        StateDirty();
    }
}
