using System.Threading;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules;

public sealed class MaxTimeRestartRuleSystem : GameRuleSystem<MaxTimeRestartRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(RunLevelChanged);
    }

    protected override void Started(EntityUid uid, MaxTimeRestartRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if(GameTicker.RunLevel == GameRunLevel.InRound)
            RestartTimer(uid, component); // DS14
    }

    protected override void Ended(EntityUid uid, MaxTimeRestartRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        StopTimer(component);
    }

    public void RestartTimer(EntityUid uid, MaxTimeRestartRuleComponent component) // DS14
    {
        // TODO FULL GAME SAVE
        component.TimerCancel.Cancel();
        component.TimerCancel = new CancellationTokenSource();
        // DS14-start
        var roundId = GameTicker.RoundId;
        Timer.Spawn(component.RoundMaxTime, () => TimerFired(uid, roundId), component.TimerCancel.Token);
        // DS14-end
    }

    public void StopTimer(MaxTimeRestartRuleComponent component)
    {
        component.TimerCancel.Cancel();
    }

    private void TimerFired(EntityUid uid, int roundId) // DS14
    {
        // DS14-start
        if (!TryComp(uid, out MaxTimeRestartRuleComponent? component) ||
            !GameTicker.IsGameRuleActive(uid) ||
            GameTicker.RoundId != roundId ||
            GameTicker.RunLevel != GameRunLevel.InRound)
        {
            return;
        }

        var roundEndDelay = component.RoundEndDelay;
        // DS14-end

        GameTicker.EndRound(Loc.GetString("rule-time-has-run-out"));

        // DS14-start
        if (GameTicker.RoundId != roundId || GameTicker.RunLevel != GameRunLevel.PostRound)
            return;
        // DS14-end

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds",("seconds", (int) roundEndDelay.TotalSeconds))); // DS14

        // TODO FULL GAME SAVE
        // DS14-start
        // Once the round has ended, restarting is no longer owned by the game-rule entity.
        Timer.Spawn(roundEndDelay, () =>
        {
            if (GameTicker.RoundId == roundId && GameTicker.RunLevel == GameRunLevel.PostRound)
                GameTicker.RestartRound();
        });
        // DS14-end
    }

    private void RunLevelChanged(GameRunLevelChangedEvent args)
    {
        var query = EntityQueryEnumerator<MaxTimeRestartRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var timer, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue; // DS14

            switch (args.New)
            {
                case GameRunLevel.InRound:
                    RestartTimer(uid, timer); // DS14
                    break;
                case GameRunLevel.PreRoundLobby:
                case GameRunLevel.PostRound:
                    StopTimer(timer);
                    break;
            }
        }
    }
}
