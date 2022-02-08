using System;
using System.Collections.Generic;
using Content.Server.Dynamic.Prototypes;
using Robust.Shared.Prototypes;
using Logger = Robust.Shared.Log.Logger;

namespace Content.Server.Dynamic.Systems;

public partial class DynamicModeSystem
{
    /// <summary>
    ///     A data structure containing the accumulators for each active scheduler.
    /// </summary>
    public Dictionary<DynamicSchedulerPrototype, float> SchedulerTiming = new();

    /// <summary>
    ///     A data structure mapping each scheduler to the list of events that it can
    /// </summary>
    public Dictionary<DynamicSchedulerPrototype, List<GameEventPrototype>> Schedulers = new();

    public void ReloadSchedulers(PrototypesReloadedEventArgs args)
    {
        RebuildSchedulers();
    }

    public void RebuildSchedulers()
    {
        Schedulers = new();
        foreach (var ev in _proto.EnumeratePrototypes<GameEventPrototype>())
        {
            if (ev.EventType != DynamicEventType.Midround)
                continue;

            if (!_proto.TryIndex<DynamicSchedulerPrototype>(ev.Scheduler, out var scheduler))
            {
                Logger.Error($"Game event {ev.Name} specified nonexistent scheduler {ev.Scheduler}");
                continue;
            }

            if (!Schedulers.ContainsKey(scheduler))
            {
                Schedulers.Add(scheduler, new () { ev });
            }
            else
            {
                Schedulers[scheduler].Add(ev);
            }

            if (!SchedulerTiming.ContainsKey(scheduler))
            {
                SchedulerTiming.Add(scheduler, 0.0f);
            }
        }
    }

    public void ScheduleMidroundInjection(float frameTime)
    {
        var roundTime = _gameTicker.RoundDuration();
        foreach (var (schedule, _) in SchedulerTiming)
        {
            if (roundTime < TimeSpan.FromSeconds(schedule.MinRoundTime)
                || roundTime > TimeSpan.FromSeconds(schedule.MaxRoundTime))
                continue;

            SchedulerTiming[schedule] += frameTime;
            if (SchedulerTiming[schedule] > schedule.Frequency)
            {
                SchedulerTiming[schedule] -= schedule.Frequency;
            }
            else
            {
                continue;
            }

            if (Schedulers.TryGetValue(schedule, out var events))
            {
                // pick one
                TryRunMidroundEvent(events, schedule.ID);
            }
        }
    }
}
