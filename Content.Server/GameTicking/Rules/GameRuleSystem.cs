using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> : EntitySystem where T : IComponent
{
    [Dependency] protected readonly IChatManager ChatManager = default!;
    [Dependency] protected readonly GameTicker GameTicker = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, GameRuleAddedEvent>(OnGameRuleAdded);
        SubscribeLocalEvent<T, GameRuleStartedEvent>(OnGameRuleStarted);
        SubscribeLocalEvent<T, GameRuleEndedEvent>(OnGameRuleEnded);
    }

    private void OnGameRuleAdded(EntityUid uid, T component, ref GameRuleAddedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Added(uid, component, ruleData, args);
    }

    private void OnGameRuleStarted(EntityUid uid, T component, ref GameRuleStartedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Started(uid, component, ruleData, args);
    }

    private void OnGameRuleEnded(EntityUid uid, T component, ref GameRuleEndedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Ended(uid, component, ruleData, args);
    }


    /// <summary>
    /// Called when the gamerule is added
    /// </summary>
    protected virtual void Added(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {

    }

    /// <summary>
    /// Called when the gamerule begins
    /// </summary>
    protected virtual void Started(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {

    }

    /// <summary>
    /// Called when the gamerule ends
    /// </summary>
    protected virtual void Ended(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {

    }

    /// <summary>
    /// Called on an active gamerule entity in the Update function
    /// </summary>
    protected virtual void ActiveTick(EntityUid uid, T component, GameRuleComponent gameRule, float frameTime)
    {
        var now = Timing.CurTime;
        if (gameRule.ScheduledTasks.Count > 0 && gameRule.ScheduledTasks.GetKeyAtIndex(0) <= now)
        {
            var task = gameRule.ScheduledTasks.GetValueAtIndex(0);
            task.Action(uid, component, gameRule, frameTime);
            gameRule.ScheduledTasks.RemoveAt(0);

            if (!task.Oneshot && task.Interval.HasValue)
                gameRule.ScheduledTasks.Add(now + task.Interval.Value, task);
        }
    }

    protected EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent> QueryActiveRules()
    {
        return EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
    }

    protected bool TryRoundStartAttempt(RoundStartAttemptEvent ev, string localizedPresetName)
    {
        var query = EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var _, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                return false;

            if (ev.Players.Length == 0)
            {
                ChatManager.DispatchServerAnnouncement(Loc.GetString("preset-no-one-ready", ("presetName", localizedPresetName)));
                ev.Cancel();
                continue;
            }

            var minPlayers = gameRule.MinPlayers;
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                ChatManager.SendAdminAnnouncement(Loc.GetString("preset-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length),
                    ("minimumPlayers", minPlayers),
                    ("presetName", localizedPresetName)));
                ev.Cancel();
                continue;
            }
        }

        return !ev.Cancelled;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<T, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2))
        {
            if (!GameTicker.IsGameRuleActive(uid, comp2))
                continue;

            ActiveTick(uid, comp1, comp2, frameTime);
        }
    }

    /// <summary>
    /// Schedule a task to run repeatedly every interval
    /// </summary>
    /// <param name="task">An action accepting : Rule entity, T Component, GameRuleComponent and frameTime</param>
    /// <param name="interval">Timespan specifying the interval to run the task</param>
    public void ScheduleRecurringTask(Action<EntityUid, IComponent, GameRuleComponent, float> task, TimeSpan interval, GameRuleComponent gameRule)
    {
        var now = Timing.CurTime;
        gameRule.ScheduledTasks.Add(now + interval, new GameRuleTask(task, false, interval));
    }
    /// <summary>
    /// Schedule a task to be run once after a delay
    /// </summary>
    /// <param name="task">An action accepting : Rule entity, T Component, GameRuleComponent and frameTime</param>
    /// <param name="delay">Timespan specifying how long to delay before running the task</param>
    public void ScheduleOneshotTask(Action<EntityUid, IComponent, GameRuleComponent, float> task, TimeSpan delay, GameRuleComponent gameRule)
    {
        var now = Timing.CurTime;
        gameRule.ScheduledTasks.Add(now + delay, new GameRuleTask(task, true));
    }
}
