using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed partial class MaxTimeRestartRuleSystem : GameRuleSystem<MaxTimeRestartRuleComponent>
{
    private static readonly EntityTimerId RoundTimer = new("round-limit");
    private static readonly EntityTimerId RestartTimerId = new("restart-round");

    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(RunLevelChanged);
        SubscribeLocalEvent<MaxTimeRestartRuleComponent, EntityTimerEvent>(OnTimer);
    }

    protected override void Started(EntityUid uid, MaxTimeRestartRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if(GameTicker.RunLevel == GameRunLevel.InRound)
            RestartTimer(uid, component);
    }

    protected override void Ended(EntityUid uid, MaxTimeRestartRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        StopTimer(uid);
    }

    public void RestartTimer(EntityUid uid, MaxTimeRestartRuleComponent component)
    {
        // TODO FULL GAME SAVE
        _timers.SetTimer<MaxTimeRestartRuleComponent>((uid, component), RoundTimer, component.RoundMaxTime);
    }

    public void StopTimer(EntityUid uid)
    {
        _timers.CancelTimers<MaxTimeRestartRuleComponent>(uid);
    }

    private void OnTimer(Entity<MaxTimeRestartRuleComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id == RestartTimerId)
        {
            GameTicker.RestartRound();
            return;
        }

        if (args.Id != RoundTimer)
            return;

        GameTicker.EndRound(Loc.GetString("rule-time-has-run-out"));

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds",("seconds", (int) ent.Comp.RoundEndDelay.TotalSeconds)));

        // TODO FULL GAME SAVE
        _timers.SetTimer(ent, RestartTimerId, ent.Comp.RoundEndDelay);
    }

    private void RunLevelChanged(GameRunLevelChangedEvent args)
    {
        var query = EntityQueryEnumerator<MaxTimeRestartRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var timer, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                return;

            switch (args.New)
            {
                case GameRunLevel.InRound:
                    RestartTimer(uid, timer);
                    break;
                case GameRunLevel.PreRoundLobby:
                case GameRunLevel.PostRound:
                    StopTimer(uid);
                    break;
            }
        }
    }
}
