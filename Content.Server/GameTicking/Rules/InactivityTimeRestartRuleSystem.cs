using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed partial class InactivityTimeRestartRuleSystem : GameRuleSystem<InactivityRuleComponent>
{
    private static readonly EntityTimerId InactivityTimer = new("inactivity");
    private static readonly EntityTimerId RestartTimerId = new("restart-round");

    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(RunLevelChanged);
        SubscribeLocalEvent<InactivityRuleComponent, EntityTimerEvent>(OnTimer);
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

        _timers.SetTimer<InactivityRuleComponent>((uid, component), InactivityTimer, component.InactivityMaxTime);
    }

    public void StopTimer(EntityUid uid, InactivityRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _timers.CancelTimers<InactivityRuleComponent>(uid);
    }

    private void OnTimer(Entity<InactivityRuleComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id == RestartTimerId)
        {
            GameTicker.RestartRound();
            return;
        }

        if (args.Id != InactivityTimer)
            return;

        GameTicker.EndRound(Loc.GetString("rule-time-has-run-out"));

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds", ("seconds",(int) ent.Comp.RoundEndDelay.TotalSeconds)));

        _timers.SetTimer(ent, RestartTimerId, ent.Comp.RoundEndDelay);
    }

    private void RunLevelChanged(GameRunLevelChangedEvent args)
    {
        var query = EntityQueryEnumerator<InactivityRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var inactivity, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                return;

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
                return;

            if (GameTicker.RunLevel != GameRunLevel.InRound)
            {
                return;
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
