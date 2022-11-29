using System.Linq;
using Content.Server.Connection;
using Content.Shared.CCVar;
using Content.Shared.Corvax.JoinQueue;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.Corvax.JoinQueue;

/// <summary>
///     Manages new player connections when the server is full and queues them up, granting access when a slot becomes free
/// </summary>
public sealed class JoinQueueManager
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConnectionManager _connectionManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;

    /// <summary>
    ///     Queue of active player sessions
    /// </summary>
    private readonly List<IPlayerSession> _queue = new(); // Real Queue class can't delete disconnected users

    private bool _isEnabled = false;

    public int PlayerInQueueCount => _queue.Count;
    public int ActualPlayersCount => _playerManager.PlayerCount - PlayerInQueueCount; // Now it's only real value with actual players count that in game
    
    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgQueueUpdate>();
        
        _cfg.OnValueChanged(CCVars.QueueEnabled, OnQueueCVarChanged, true);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void OnQueueCVarChanged(bool value)
    {
        _isEnabled = value;

        if (!value)
        {
            foreach (var session in _queue)
            {
                session.ConnectedClient.Disconnect("Queue was disabled");
            }
        }
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Connected:
            {
                if (!_isEnabled)
                {
                    SendToGame(e.Session);
                    return;
                }
                
                var isPrivileged = await _connectionManager.HavePrivilegedJoin(e.Session.UserId);
                var haveFreeSlot = _playerManager.PlayerCount < _cfg.GetCVar(CCVars.SoftMaxPlayers);
                if (isPrivileged || haveFreeSlot)
                {
                    SendToGame(e.Session);
                    return;
                }
                
                _queue.Add(e.Session);
                ProcessQueue(false);
                break;
            }
            case SessionStatus.Disconnected:
            {
                _queue.Remove(e.Session);
                ProcessQueue(true);
                break;
            }
        }
    }

    /// <summary>
    ///     If possible, takes the first player in the queue and sends him into the game
    /// </summary>
    private void ProcessQueue(bool isDisconnect)
    {
        var players = ActualPlayersCount;
        if (isDisconnect)
            players--; // Decrease currently disconnected session but that has not yet been deleted
        
        var haveFreeSlot = players < _cfg.GetCVar(CCVars.SoftMaxPlayers);
        var queueContains = _queue.Count > 0;
        if ((!_isEnabled || haveFreeSlot) && queueContains)
        {
            var session = _queue.First();
            _queue.Remove(session);
            SendToGame(session);
        }

        SendUpdateMessages();
    }

    /// <summary>
    ///     Sends messages to all players in the queue with the current state of the queue
    /// </summary>
    private void SendUpdateMessages()
    {
        for (var i = 0; i < _queue.Count; i++)
        {
            _queue[i].ConnectedClient.SendMessage(new MsgQueueUpdate
            {
                Total = _queue.Count,
                Position = i + 1,
            });
        }
    }

    /// <summary>
    ///     Letting player's session into game, change player state
    /// </summary>
    private void SendToGame(IPlayerSession s)
    {
        Timer.Spawn(0, s.JoinGame);
    }
}