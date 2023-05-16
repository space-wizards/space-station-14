using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Metric;
using Content.Shared.chaos;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.StationEvents
{
    /// <summary>
    ///     A scheduler which tries to keep station chaos within a set bound over time with the most suitable
    ///        good or bad events to nudge it in the correct direction.
    /// </summary>
    [UsedImplicitly]
    public sealed class DynamicStationEventSchedulerSystem : GameRuleSystem<DynamicStationEventSchedulerComponent>
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly EventManagerSystem _event = default!;
        [Dependency] private readonly StationMetricSystem _metrics = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        protected override void Added(EntityUid uid, DynamicStationEventSchedulerComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
        {
            _metrics.SetupMetrics();
        }

        protected override void Ended(EntityUid uid, DynamicStationEventSchedulerComponent component, GameRuleComponent gameRule,
            GameRuleEndedEvent args)
        {
            component.TimeUntilNextEvent = BasicStationEventSchedulerComponent.MinimumTimeUntilFirstEvent;
        }

        protected override void ActiveTick(EntityUid uid, DynamicStationEventSchedulerComponent scheduler, GameRuleComponent gameRule, float frameTime)
        {
            if (scheduler.TimeUntilNextEvent > 0)
            {
                scheduler.TimeUntilNextEvent -= frameTime;
                return;
            }

            var chaos = _metrics.CalculateChaos();
            _adminLogger.Add(LogType.DynamicRule, LogImpact.Low, $"Station chaos is now {chaos.ChaosDict.ToString()}");

            ResetTimer(scheduler);
        }

        /// <summary>
        /// Reset the event timer once the event is done.
        /// </summary>
        private void ResetTimer(DynamicStationEventSchedulerComponent scheduler)
        {
            // For testing, 30 secs.
            scheduler.TimeUntilNextEvent = 30f;

            // // 4 - 12 minutes.
            // scheduler.TimeUntilNextEvent = _random.NextFloat(240f, 720f);
        }
    }
}
