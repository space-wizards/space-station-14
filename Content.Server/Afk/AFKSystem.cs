using System.Linq;
using Content.Server.Afk.Events;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Afk;

/// <summary>
/// Actively checks for AFK players regularly and issues an event whenever they go afk.
/// </summary>
public sealed class AFKSystem : EntitySystem
{
    [Dependency] private readonly IAfkManager _afkManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;

    private float _checkDelay;
    private float _kickDelay;
    private float _accumulator;

    private readonly Dictionary<IPlayerSession, double> _afkPlayers = new();

    public override void Initialize()
    {
        base.Initialize();
        _playerManager.PlayerStatusChanged += OnPlayerChange;
        _configManager.OnValueChanged(CCVars.AfkTime, SetAfkDelay, true);
        _configManager.OnValueChanged(CCVars.AfkKickTime, SetAfkKickDelay, true);
    }

    private void SetAfkDelay(float obj)
    {
        _checkDelay = obj;
    }
    private void SetAfkKickDelay(float obj)
    {
        _kickDelay = obj;
    }

    private void OnPlayerChange(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Disconnected:
                _afkPlayers.Remove(e.Session);
                break;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _afkPlayers.Clear();
        _accumulator = 0f;
        _playerManager.PlayerStatusChanged -= OnPlayerChange;
        _configManager.UnsubValueChanged(CCVars.AfkTime, SetAfkDelay);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_ticker.RunLevel != GameRunLevel.InRound)
        {
            _afkPlayers.Clear();
            _accumulator = 0f;
            return;
        }

        _accumulator += frameTime;

        // TODO: Should also listen to the input events for more accurate timings.
        if (_accumulator < _checkDelay) return;

        _accumulator -= _checkDelay;

        foreach (var session in Filter.GetAllPlayers())
        {
            var pSession = (IPlayerSession) session;
            var isAfk = _afkManager.IsAfk(pSession);
            if (isAfk)
            {
                var curTime = _gameTiming.CurTime.TotalSeconds;
                if (!_afkPlayers.ContainsKey(pSession))
                {
                    _afkPlayers.Add(pSession, curTime);
                    var ev = new AFKEvent(pSession);
                    RaiseLocalEvent(ref ev);
                    continue;
                }
                var timeElapsed = curTime - _afkPlayers[pSession];
                if (timeElapsed > _kickDelay)
                {
                    _netManager.DisconnectChannel(pSession.ConnectedClient, Loc.GetString("afk-system-kick-reason"));
                }
                else if(timeElapsed >= _kickDelay-1)
                {
                    _chatManager.DispatchServerMessage(pSession, Loc.GetString("afk-system-kick-warning"), Color.Red);
                }
            }
            else if (_afkPlayers.ContainsKey(pSession))
            {
                _afkPlayers.Remove(pSession);
                var ev = new UnAFKEvent(pSession);
                RaiseLocalEvent(ref ev);
                continue;
            }
        }
    }
}
