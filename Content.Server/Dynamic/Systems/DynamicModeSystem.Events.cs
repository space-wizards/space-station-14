using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Database;
using Content.Server.Dynamic.Prototypes;
using Content.Server.Mind.Components;
using Content.Shared.CCVar;
using NFluidsynth;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.ViewVariables;
using Logger = Robust.Shared.Log.Logger;

namespace Content.Server.Dynamic.Systems;

public partial class DynamicModeSystem
{
    /// <summary>
    ///     Holds the number of times each event has been run.
    /// </summary>
    [ViewVariables]
    public Dictionary<GameEventPrototype, int> TotalEvents = new();

    /// <summary>
    ///     Holds the active events, with the value representing
    ///     when the event started, for refunding purposes.
    /// </summary>
    [ViewVariables]
    public List<(GameEventPrototype, TimeSpan)> ActiveEvents = new();

    #region Latejoin

    private float _latejoinAccumulator;

    /// <summary>
    ///     When will dynamic start accepting latejoin events?
    /// </summary>
    public TimeSpan LatejoinStart => TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.DynamicLatejoinInjectStart));

    /// <summary>
    ///     When will dynamic stop accepting latejoin events?
    /// </summary>
    public TimeSpan LatejoinEnd => TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.DynamicLatejoinInjectEnd));

    #endregion

    #region Midround

    private float _midroundAccumulator;

    /// <summary>
    ///     When will dynamic start accepting latejoin events?
    /// </summary>
    public TimeSpan MidroundStart => TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.DynamicMidroundStart));

    /// <summary>
    ///     When will dynamic stop accepting latejoin events?
    /// </summary>
    public TimeSpan MidroundEnd => TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.DynamicMidroundEnd));

    #endregion

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
            // TODO weighted pick.
            var ev = _random.Pick(protos);
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
    ///     Returns a hashset of <see cref="Candidate"/> given a list of players,
    ///     first determining if they are valid candidates
    /// </summary>
    /// <param name="proto"></param>
    /// <param name="players"></param>
    /// <returns></returns>
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
        ActiveEvents.Add((proto, _gameTiming.CurTime));
        if (TotalEvents.ContainsKey(proto))
            TotalEvents[proto] += 1;
        else
            TotalEvents.Add(proto, 1);

        foreach (var effect in proto.EventEffects)
        {
            effect.Effect(data, EntityManager);
        }
    }
}

public class StartEventAttemptEvent : CancellableEntityEventArgs
{
    public GameEventData Data;
    public GameEventPrototype Prototype;

    public StartEventAttemptEvent(GameEventData data, GameEventPrototype prototype)
    {
        Data = data;
        Prototype = prototype;
    }
}
