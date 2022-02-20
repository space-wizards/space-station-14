using System;
using System.Threading;
using Content.Server.Chat.Managers;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules;

public sealed class InactivityTimeRestartRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Prototype => "InactivityTimeRestart";

    private CancellationTokenSource _timerCancel = new();

    public TimeSpan InactivityMaxTime { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan RoundEndDelay { get; set; } = TimeSpan.FromSeconds(10);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(RunLevelChanged);
    }

    public override void Started()
    {
        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
    }

    public override void Ended()
    {
        _playerManager.PlayerStatusChanged -= PlayerStatusChanged;

        StopTimer();
    }

    public void RestartTimer()
    {
        _timerCancel.Cancel();
        _timerCancel = new CancellationTokenSource();
        Timer.Spawn(InactivityMaxTime, TimerFired, _timerCancel.Token);
    }

    public void StopTimer()
    {
        _timerCancel.Cancel();
    }

    private void TimerFired()
    {
        GameTicker.EndRound(Loc.GetString("rule-time-has-run-out"));

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds", ("seconds",(int) RoundEndDelay.TotalSeconds)));

        Timer.Spawn(RoundEndDelay, () => GameTicker.RestartRound());
    }

    private void RunLevelChanged(GameRunLevelChangedEvent args)
    {
        if (!Enabled)
            return;

        switch (args.New)
        {
            case GameRunLevel.InRound:
                RestartTimer();
                break;
            case GameRunLevel.PreRoundLobby:
            case GameRunLevel.PostRound:
                StopTimer();
                break;
        }
    }

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (GameTicker.RunLevel != GameRunLevel.InRound)
        {
            return;
        }

        if (_playerManager.PlayerCount == 0)
        {
            RestartTimer();
        }
        else
        {
            StopTimer();
        }
    }
}
