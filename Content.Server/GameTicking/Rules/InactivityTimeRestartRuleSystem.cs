using System.Threading;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules;

public sealed class InactivityTimeRestartRuleSystem : GameRuleSystem<InactivityRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(RunLevelChanged);
        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
    }

    protected override void Ended(EntityUid uid, InactivityRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        StopTimer(uid, component);
    }

    public void RestartTimer(EntityUid uid, InactivityRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.TimerCancel.Cancel();
        component.TimerCancel = new CancellationTokenSource();
        // DS14-start
        var roundId = GameTicker.RoundId;
        Timer.Spawn(component.InactivityMaxTime, () => TimerFired(uid, roundId), component.TimerCancel.Token);
        // DS14-end
    }

    public void StopTimer(EntityUid uid, InactivityRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.TimerCancel.Cancel();
    }

    private void TimerFired(EntityUid uid, int roundId) // DS14
    {
        // DS14-start
        if (!TryComp(uid, out InactivityRuleComponent? component) || !GameTicker.IsGameRuleActive(uid))
            return;

        if (GameTicker.RoundId != roundId || GameTicker.RunLevel != GameRunLevel.InRound)
            return;
        // DS14-end

        var roundEndDelay = component.RoundEndDelay; // DS14

        GameTicker.EndRound(Loc.GetString("rule-time-has-run-out"));

        // DS14-start
        if (GameTicker.RoundId != roundId || GameTicker.RunLevel != GameRunLevel.PostRound)
            return;
        // DS14-end

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds", ("seconds",(int) roundEndDelay.TotalSeconds))); // DS14

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
        var query = EntityQueryEnumerator<InactivityRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var inactivity, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue; // DS14

            switch (args.New)
            {
                case GameRunLevel.InRound:
                    RestartTimer(uid, inactivity);
                    break;
                case GameRunLevel.PreRoundLobby:
                case GameRunLevel.PostRound:
                    StopTimer(uid, inactivity);
                    break;
            }
        }
    }

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        var query = EntityQueryEnumerator<InactivityRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var inactivity, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue; // DS14

            if (GameTicker.RunLevel != GameRunLevel.InRound)
            {
                continue; // DS14
            }

            if (_playerManager.PlayerCount == 0)
            {
                RestartTimer(uid, inactivity);
            }
            else
            {
                StopTimer(uid, inactivity);
            }
        }
    }
}
