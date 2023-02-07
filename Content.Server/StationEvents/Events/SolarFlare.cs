using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Radio.Components;
using Content.Server.Radio;

namespace Content.Server.StationEvents.Events
{
    public sealed class SolarFlare : StationEventSystem
    {
        public override string Prototype => "SolarFlare";

        private bool _onlyJamHeadsets = true;
        private HashSet<string> _affectedChannels = new();
        private float _endAfter = 0.0f;
        private bool _running = false;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActiveRadioComponent, RadioReceiveAttemptEvent>(OnRadioSendAttempt);
        }

        public override void Added()
        {
            if (Configuration is not SolarFlareEventRuleConfiguration ev)
                return;
            base.Added();
            _onlyJamHeadsets = ev.OnlyJamHeadsets;
            _endAfter = RobustRandom.Next(ev.MinEndAfter, ev.MaxEndAfter);
            _affectedChannels = ev.AffectedChannels;
        }

        public override void Started()
        {
            base.Started();
            _running = true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!RuleStarted)
                return;

            if (Elapsed > _endAfter)
            {
                ForceEndSelf();
                return;
            }
        }

        public override void Ended()
        {
            base.Ended();
            _running = false;
        }

        private void OnRadioSendAttempt(EntityUid uid, ActiveRadioComponent component, RadioReceiveAttemptEvent args)
        {
            if (_running && _affectedChannels.Contains(args.Channel.ID))
                if (!_onlyJamHeadsets || (HasComp<HeadsetComponent>(uid) || HasComp<HeadsetComponent>(args.RadioSource)))
                    args.Cancel();
        }

    }
}