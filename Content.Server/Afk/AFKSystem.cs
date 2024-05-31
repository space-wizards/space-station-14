using Content.Server.Afk.Events;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Input;
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

    private float _checkDelay;
    private TimeSpan _checkTime;

    private readonly HashSet<ICommonSession> _afkPlayers = new();

    public override void Initialize()
    {
        base.Initialize();
        _playerManager.PlayerStatusChanged += OnPlayerChange;
        Subs.CVar(_configManager, CCVars.AfkTime, SetAfkDelay, true);

        SubscribeNetworkEvent<FullInputCmdMessage>(HandleInputCmd);
    }

    private void HandleInputCmd(FullInputCmdMessage msg, EntitySessionEventArgs args)
    {
        _afkManager.PlayerDidAction(args.SenderSession);
    }

    private void SetAfkDelay(float obj)
    {
        _checkDelay = obj;
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

        foreach (var pSession in Filter.GetAllPlayers())
        {
            if (pSession.Status != SessionStatus.InGame) continue;
            var isAfk = _afkManager.IsAfk(pSession);

            if (isAfk && _afkPlayers.Add(pSession))
            {
                var ev = new AFKEvent(pSession);
                RaiseLocalEvent(ref ev);
                continue;
            }

            if (!isAfk && _afkPlayers.Remove(pSession))
            {
                var ev = new UnAFKEvent(pSession);
                RaiseLocalEvent(ref ev);
            }
        }
    }
}
