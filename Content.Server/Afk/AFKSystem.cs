using System.Linq;
using Content.Server.Afk.Events;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    private float _checkDelay;
    private float _kickDelay;
    private TimeSpan _checkTime;

    private readonly Dictionary<IPlayerSession, TimeSpan> _afkPlayers = new();

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
        _playerManager.PlayerStatusChanged -= OnPlayerChange;
        _configManager.UnsubValueChanged(CCVars.AfkTime, SetAfkDelay);
        _configManager.UnsubValueChanged(CCVars.AfkKickTime, SetAfkKickDelay);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_ticker.RunLevel != GameRunLevel.InRound)
        {
            _afkPlayers.Clear();
            _checkTime = TimeSpan.Zero;
            return;
        }

        // TODO: Should also listen to the input events for more accurate timings.
        if (_timing.CurTime < _checkTime)
            return;

        _checkTime = _timing.CurTime + TimeSpan.FromSeconds(_checkDelay);

        foreach (var session in Filter.GetAllPlayers())
        {
            var pSession = (IPlayerSession) session;
            var isAfk = _afkManager.IsAfk(pSession);

            if (isAfk && _afkPlayers.TryAdd(pSession, _timing.CurTime))
            {
                var ev = new AFKEvent(pSession);
                RaiseLocalEvent(ref ev);
                
                _chatManager.DispatchServerMessage(pSession, Loc.GetString("afk-system-kick-warning"));
            }

            if (!isAfk && _afkPlayers.Remove(pSession))
            {
                var ev = new UnAFKEvent(pSession);
                RaiseLocalEvent(ref ev);
            }

            if (isAfk &&
                _afkPlayers.TryGetValue(pSession, out var startAfkTime) &&
                _timing.CurTime - startAfkTime >= TimeSpan.FromSeconds(_kickDelay))
            {
                pSession.ConnectedClient.Disconnect( Loc.GetString("afk-system-kick-reason"));
            }
        }
    }
}
