using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
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
using static Content.Shared.Administration.AdminLogsEuiMsg;

namespace Content.Server.Administration.UI;

public sealed class AdminLogsEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private readonly AdminLogSystem _logSystem;

    private int _clientBatchSize;
    private bool _isLoading = true;
    private readonly Dictionary<Guid, string> _players = new();
    private CancellationTokenSource _logSendCancellation = new();
    private LogFilter _filter;

    public AdminLogsEui()
    {
        IoCManager.InjectDependencies(this);

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
                _logSendCancellation.Cancel();
                _logSendCancellation = new CancellationTokenSource();
                _filter = new LogFilter
                {
                    CancellationToken = _logSendCancellation.Token,
                    Round = request.RoundId,
                    Types = request.Types,
                    Before = request.Before,
                    After = request.After,
                    AnyPlayers = request.AnyPlayers,
                    AllPlayers = request.AllPlayers,
                    LastLogId = 0,
                    Limit = _clientBatchSize
                };

                var roundId = _filter.Round ??= EntitySystem.Get<GameTicker>().RoundId;
                LoadFromDb(roundId);

                var logs = await Task.Run(() => _logSystem.All(_filter));
                SendLogs(logs, true);
                break;
            }
            case NextLogsRequest:
            {
                var results = await Task.Run(() => _logSystem.All(_filter));
                SendLogs(results, false);
                break;
            }
        }
    }

    private async void SendLogs(IAsyncEnumerable<LogRecord> enumerable, bool replace)
    {
        var logs = new List<SharedAdminLog>(_clientBatchSize);

        await Task.Run(async () =>
        {
            await foreach (var record in enumerable.WithCancellation(_logSendCancellation.Token))
            {
                var log = new SharedAdminLog(record.Id, record.Date, record.Message);
                logs.Add(log);
            }
        });

        if (logs.Count > 0)
        {
            _filter.LastLogId = logs[^1].Id;
        }

        var message = new NewLogs(logs.ToArray(), replace);

        SendMessage(message);
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
