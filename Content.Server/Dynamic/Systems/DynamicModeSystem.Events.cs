using System.Linq;
using Content.Server.Dynamic.Prototypes;
using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Dynamic.Systems;

public partial class DynamicModeSystem
{
    /// <summary>
    ///     Holds the number of times each event has been run.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<GameEventPrototype, int> TotalEvents = new();

    /// <summary>
    ///     Holds the active events, with the value representing
    ///     when the event started, for refunding purposes.
    /// </summary>
    [ViewVariables]
    public readonly List<(GameEventPrototype, TimeSpan)> RefundableEvents = new();

    /// <summary>
    ///     When will dynamic start accepting latejoin events?
    /// </summary>
    public TimeSpan LatejoinStart => TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.DynamicLatejoinInjectStart));

    /// <summary>
    ///     When will dynamic stop accepting latejoin events?
    /// </summary>
    public TimeSpan LatejoinEnd => TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.DynamicLatejoinInjectEnd));

    /// <summary>
    ///     Above this value, latejoin injection chance is increased.
    /// </summary>
    public float HighInjectionChanceThreshold = 70.0f;

    /// <summary>
    ///     Below this value, latejoin injection chance is decreased.
    /// </summary>
    public float LowInjectionChanceThreshold = 10.0f;

    public void InitializeEvents()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        // We only care about latejoins.

        if (!ev.LateJoin)
            return;

        // Should we actually run latejoin checks yet?
        var roundDur = _gameTicker.RoundDuration();
        if (roundDur < LatejoinStart || roundDur > LatejoinEnd)
            return;

        TryRunLatejoinEvent(ev.Player);
    }

    #region Event Picking/Running

    /// <summary>
    ///     Selects a game event from a list of candidates, taking into account their weight,
    ///     some random modifiers, and the current storyteller.
    /// </summary>
    /// <param name="eventCandidates"></param>
    public GameEventPrototype SelectEvent(IEnumerable<GameEventPrototype> eventCandidates)
    {
        var chance = 0.0f;

        // TODO WEIGHTED PICK
        // TODO modify chance
        // TODO storyteller weighted pick by tag
        return _random.Pick(eventCandidates.ToArray());
    }

    /// <summary>
    ///     Purchases and starts a set of roundstart events.
    /// </summary>
    /// <param name="players"></param>
    public void RunRoundstartEvents(IPlayerSession[] players)
    {
        var protos = _proto.EnumeratePrototypes<GameEventPrototype>()
            .Where(p => p.EventType == DynamicEventType.Roundstart).ToList();

        if (protos.Count == 0)
        {
            Logger.Info("dynamic couldnt pick jack shit");
            return;
        }

        var playerCount = players.Length;
        while (RoundstartBudget > 0)
        {
            var ev = SelectEvent(protos);
            var candidates = GetCandidates(ev, players);
            var evData = new GameEventData(playerCount, candidates);
            if (!CanStartEvent(ev, evData))
                continue;

            Logger.Info($"dynamic picked event {ev.Name}, new budget {RoundstartBudget}");

            RoundstartBudget = Math.Max(RoundstartBudget - ev.ThreatCost, 0);
            StartEvent(ev, evData);
        }
    }

    /// <summary>
    ///     Attempts to run a midround event.
    ///     Takes in a list of event candidates which are determined by the scheduler responsible for them.
    ///     A midround event with no scheduler will not be run.
    /// </summary>
    public void TryRunMidroundEvent(List<GameEventPrototype> eventCandidates, string schedule)
    {
        var cand = SelectEvent(eventCandidates);
        Logger.Info($"dynamic midround picked {cand.Name} on schedule {schedule}");
    }

    /// <summary>
    ///     For a given latejoiner, tries to run a latejoin event.
    /// </summary>
    public void TryRunLatejoinEvent(IPlayerSession player)
    {
        // TODO
        var events = _proto.EnumeratePrototypes<GameEventPrototype>()
            .Where(e => e.EventType == DynamicEventType.Latejoin);

        var cand = SelectEvent(events);
        Logger.Info($"Tried to run latejoin event {cand.Name}, weehee");
    }

    #endregion

    /// <summary>
    ///     Iterates over all active refundable events and checks if they can be refunded.
    ///     If they've gone past the max time, remove it from the list instead.
    /// </summary>
    public void CheckAvailableRefunds()
    {
        RemQueue<(GameEventPrototype, TimeSpan)> remove = new();
        foreach (var item in RefundableEvents.ToArray())
        {
            var (ev, time) = item;
            if (_gameTiming.CurTime - time >= TimeSpan.FromSeconds(ev.MaxRefundTime)
                || ev.RefundConditions == null)
            {
                remove.Add(item);
                continue;
            }

            var shouldRefund = true;

            // Refund conditions don't actually have a list of candidates.
            var data = new GameEventData(GameTicker.PlayersInGame.Count, new());
            foreach (var cond in ev.RefundConditions)
            {
                // If any fail, don't refund
                if (!cond.Condition(data, EntityManager))
                {
                    shouldRefund = false;
                    break;
                }
            }

            if (shouldRefund)
            {
                // Always add it to the midround pool because it doesn't
                // really make sense to refund roundstart events. And if a roundstart event -does- get refunded,
                // it should just get added to the midround pool anyway so it doesn't get wasted.

                MidroundBudget += ev.ThreatCost;
                MidroundBudget = Math.Min(MidroundBudget, ThreatLevel);
                remove.Add(item);
            }
        }

        foreach (var item in remove)
        {
            RefundableEvents.Remove(item);
        }
    }

    #region Utility

    /// <summary>
    ///     Returns a hashset of <see cref="Candidate"/> given a list of players,
    ///     first determining if they are valid candidates
    /// </summary>
    public HashSet<Candidate> GetCandidates(GameEventPrototype proto, IPlayerSession[] players)
    {
        var candidates = new HashSet<Candidate>();
        foreach (var player in players)
        {
            if (player.AttachedEntity is not { } entity)
                continue;

            if (!TryComp<MindComponent>(entity, out var mindComp)
                || (mindComp.Mind is not { } mind))
                continue;

            var cand = new Candidate(entity, mind);

            var exit = false;
            foreach (var cond in proto.CandidateConditions)
            {
                if (!cond.Condition(cand, EntityManager))
                {
                    exit = true;
                    break;
                }
            }

            if (exit)
                continue;

            candidates.Add(cand);
        }

        return candidates;
    }

    /// <summary>
    ///     Returns true if the event can be started,
    ///     false otherwise.
    /// </summary>
    public bool CanStartEvent(GameEventPrototype proto, GameEventData data)
    {
        if (proto.EventTags.Contains("HighImpact")
            && AddedHighImpact)
            return false;

        if (proto.ThreatCost >= ThreatLevel)
            return false;

        var ev = new StartEventAttemptEvent(data, proto);
        RaiseLocalEvent(ev);
        if (ev.Cancelled)
            return false;

        foreach (var cond in proto.EventConditions)
        {
            if (!cond.Condition(data, EntityManager))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Removes threat cost, starts the event, and adds it to the various collections.
    ///     Does not check if this can actually be done nor does it remove threat, so be wary.
    /// </summary>
    public void StartEvent(GameEventPrototype proto, GameEventData data)
    {
        if (proto.RefundConditions != null)
        {
            RefundableEvents.Add((proto, _gameTiming.CurTime));
        }
        if (TotalEvents.ContainsKey(proto))
            TotalEvents[proto] += 1;
        else
            TotalEvents.Add(proto, 1);

        foreach (var effect in proto.EventEffects)
        {
            effect.Effect(data, EntityManager);
        }
    }

    #endregion
}

public sealed class StartEventAttemptEvent : CancellableEntityEventArgs
{
    public GameEventData Data;
    public GameEventPrototype Prototype;

    public StartEventAttemptEvent(GameEventData data, GameEventPrototype prototype)
    {
        Data = data;
        Prototype = prototype;
    }
}
