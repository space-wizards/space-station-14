using System.Threading;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Handles rules that force the round to restart after a given time.
/// </summary>
/// <seealso cref="MaxTimeRestartRuleComponent"/>
public sealed partial class MaxTimeRestartRuleSystem : GameRuleSystem<MaxTimeRestartRuleComponent>
{
    [Dependency] private IChatManager _chatManager = default!;

    protected override void Started(Entity<MaxTimeRestartRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        if (GameTicker.RunLevel == GameRunLevel.InRound)
            RestartTimer(ent.Comp1);
    }

    protected override void Ended(Entity<MaxTimeRestartRuleComponent> rule, ref GameRuleEndedEvent args)
    {
        base.Ended(rule, ref args);

        StopTimer(rule);
    }

    public void RestartTimer(MaxTimeRestartRuleComponent component)
    {
        // TODO FULL GAME SAVE
        component.TimerCancel.Cancel();
        component.TimerCancel = new CancellationTokenSource();
        Timer.Spawn(component.RoundMaxTime, () => TimerFired(component), component.TimerCancel.Token);
    }

    public void StopTimer(MaxTimeRestartRuleComponent component)
    {
        component.TimerCancel.Cancel();
    }

    private void TimerFired(MaxTimeRestartRuleComponent component)
    {
        GameTicker.EndRound(Loc.GetString("rule-time-has-run-out"));

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds", ("seconds", (int)component.RoundEndDelay.TotalSeconds)));

        // TODO FULL GAME SAVE
        Timer.Spawn(component.RoundEndDelay, () => GameTicker.RestartRound());
    }

    [SubscribeLocalEvent]
    private void RunLevelChanged(GameRunLevelChangedEvent args)
    {
        var query = EntityQueryEnumerator<MaxTimeRestartRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var timer, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive((uid, gameRule)))
                return;

            switch (args.New)
            {
                case GameRunLevel.InRound:
                    RestartTimer(timer);
                    break;
                case GameRunLevel.PreRoundLobby:
                case GameRunLevel.PostRound:
                    StopTimer(timer);
                    break;
            }
        }
    }
}
