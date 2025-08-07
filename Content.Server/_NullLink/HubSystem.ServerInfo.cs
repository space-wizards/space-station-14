using System.Threading.Tasks;
using Content.Server._NullLink.Core;
using Content.Server._NullLink.Helpers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Players.RateLimiting;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Starlight.NullLink;
using ServerInfoRequest = Starlight.NullLink.ServerInfo;

namespace Content.Server._NullLink;

public sealed partial class HubSystem : EntitySystem
{
    private static readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(300);
    private TimeSpan _lastSent;         
    private bool _sendScheduled;   
    private int _maxPlayers;

    private ServerInfoRequest _serverInfo = new();

    public void InitializeServerInfo()
    {
        _cfg.OnValueChanged(CCVars.SoftMaxPlayers, OnSoftMaxPlayersChanged, true);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => OnLobby());
        SubscribeLocalEvent<RoundEndTextAppendEvent>(_ => OnRoundEnding());
        SubscribeLocalEvent<RoundStartingEvent>(_ => OnRoundStart());

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void OnSoftMaxPlayersChanged(int maxPlayers)
    {
        _maxPlayers = maxPlayers;
        TryUpdateServerInfo();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (_serverInfo.Players == _playerManager.PlayerCount) return;
        _serverInfo = _serverInfo with
        {
            Players = _playerManager.PlayerCount,
            MaxPlayers = _maxPlayers
        };
        TryUpdateServerInfo();
    }

    private void OnRoundStart()
    {
        _serverInfo = _serverInfo with
        {
            СurrentStateStartedAt = DateTime.UtcNow,
            Status = ServerStatus.Round,
            Players = _playerManager.PlayerCount,
            MaxPlayers = _maxPlayers,
        };
        TryUpdateServerInfo();
    }
    private void OnRoundEnding()
    {
        _serverInfo = _serverInfo with
        {
            СurrentStateStartedAt = DateTime.UtcNow,
            Status = ServerStatus.RoundEnding,
            Players = _playerManager.PlayerCount,
            MaxPlayers = _maxPlayers,
        };
        TryUpdateServerInfo();
    }
    private void OnLobby()
    {
        _serverInfo = _serverInfo with
        {
            СurrentStateStartedAt = DateTime.UtcNow,
            Status = ServerStatus.Lobby,
            Players = _playerManager.PlayerCount,
            MaxPlayers = _maxPlayers,
        };
        TryUpdateServerInfo();
    }
    private void TryUpdateServerInfo()
    {
        var now = _timing.RealTime;
        var nextAllowed = _lastSent + _minInterval;

        if (now >= nextAllowed)
        {
            Pipe.RunInBackgroundVT(SendServerInfo);
            return;
        }

        if (_sendScheduled)              
            return;

        _sendScheduled = true;
        var delay = nextAllowed - now;   

        Pipe.RunInBackground(async () =>
        {
            try
            {
                await Task.Delay(delay);
                await SendServerInfo();
            }
            finally
            {
                _sendScheduled = false; 
            }
        },
        ex => _sawmill.Log(LogLevel.Warning, ex,
            "NullLink – failed to update server info in the cluster."));
    }

    private ValueTask SendServerInfo()
    {
        _lastSent = _timing.RealTime;

        return _actors.TryGetServerGrain(out var serverGrain) 
            ? serverGrain!.UpdateServerInfo(_serverInfo) 
            : ValueTask.CompletedTask;
    }
}