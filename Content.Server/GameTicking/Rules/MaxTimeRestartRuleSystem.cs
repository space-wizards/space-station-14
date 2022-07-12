using System.Threading;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Configurations;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules;

public sealed class MaxTimeRestartRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override string Prototype => "MaxTimeRestart";

    private CancellationTokenSource _timerCancel = new();

    public TimeSpan RoundMaxTime { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RoundEndDelay { get; set; } = TimeSpan.FromSeconds(10);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(RunLevelChanged);
    }

    public override void Started()
    {
        if (Configuration is not MaxTimeRestartRuleConfiguration maxTimeRestartConfig)
            return;

        RoundMaxTime = maxTimeRestartConfig.RoundMaxTime;
        RoundEndDelay = maxTimeRestartConfig.RoundEndDelay;

        if(GameTicker.RunLevel == GameRunLevel.InRound)
            RestartTimer();
    }

    public override void Ended()
    {
        StopTimer();
    }

    public void RestartTimer()
    {
        _timerCancel.Cancel();
        _timerCancel = new CancellationTokenSource();
        Timer.Spawn(RoundMaxTime, TimerFired, _timerCancel.Token);
    }

    public void StopTimer()
    {
        _timerCancel.Cancel();
    }

    private void TimerFired()
    {
        GameTicker.EndRound(Loc.GetString("rule-time-has-run-out"));

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds",("seconds", (int) RoundEndDelay.TotalSeconds)));

        Timer.Spawn(RoundEndDelay, () => GameTicker.RestartRound());
    }

    private void RunLevelChanged(GameRunLevelChangedEvent args)
    {
        if (!RuleAdded)
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
}
