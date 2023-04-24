using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents
{
    /// <summary>
    ///     The basic event scheduler rule, loosely based off of /tg/ events, which most
    ///     game presets use.
    /// </summary>
    [UsedImplicitly]
    public sealed class BasicStationEventSchedulerSystem : GameRuleSystem
    {
        public override string Prototype => "BasicStationEventScheduler";

        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly EventManagerSystem _event = default!;

        private const float MinimumTimeUntilFirstEvent = 300;

        /// <summary>
        /// How long until the next check for an event runs
        /// </summary>
        /// Default value is how long until first event is allowed
        [ViewVariables(VVAccess.ReadWrite)]
        private float _timeUntilNextEvent = MinimumTimeUntilFirstEvent;

        public override void Started() { }

        public override void Ended()
        {
            _timeUntilNextEvent = MinimumTimeUntilFirstEvent;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!RuleStarted || !_event.EventsEnabled)
                return;

            if (_timeUntilNextEvent > 0)
            {
                _timeUntilNextEvent -= frameTime;
                return;
            }

            _event.RunRandomEvent();
            ResetTimer();
        }

        /// <summary>
        /// Reset the event timer once the event is done.
        /// </summary>
        private void ResetTimer()
        {
            // 5 - 25 minutes. TG does 3-10 but that's pretty frequent
            _timeUntilNextEvent = _random.Next(300, 1500);
        }
    }
}
