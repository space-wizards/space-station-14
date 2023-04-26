using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.StationEvents
{
    /// <summary>
    ///     The basic event scheduler rule, loosely based off of /tg/ events, which most
    ///     game presets use.
    /// </summary>
    [UsedImplicitly]
    public sealed class BasicStationEventSchedulerSystem : GameRuleSystem<BasicStationEventSchedulerComponent>
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly EventManagerSystem _event = default!;

        protected override void Ended(EntityUid uid, BasicStationEventSchedulerComponent component, GameRuleComponent gameRule,
            GameRuleEndedEvent args)
        {
            component.TimeUntilNextEvent = BasicStationEventSchedulerComponent.MinimumTimeUntilFirstEvent;
        }


        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_event.EventsEnabled)
                return;

            var query = EntityQueryEnumerator<BasicStationEventSchedulerComponent, GameRuleComponent>();
            while (query.MoveNext(out var uid, out var eventScheduler, out var gameRule))
            {
                if (!GameTicker.IsGameRuleActive(uid, gameRule))
                    continue;

                if (eventScheduler.TimeUntilNextEvent > 0)
                {
                    eventScheduler.TimeUntilNextEvent -= frameTime;
                    return;
                }

                _event.RunRandomEvent();
                ResetTimer(eventScheduler);
            }
        }

        /// <summary>
        /// Reset the event timer once the event is done.
        /// </summary>
        private void ResetTimer(BasicStationEventSchedulerComponent component)
        {
            // 5 - 25 minutes. TG does 3-10 but that's pretty frequent
            component.TimeUntilNextEvent = _random.Next(300, 1500);
        }
    }
}
