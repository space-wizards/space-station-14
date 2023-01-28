using Content.Server.Radio.Components;
using Content.Server.Radio;

namespace Content.Server.StationEvents.Events
{
    public sealed class SolarFlare : StationEventSystem
    {
        public override string Prototype => "SolarFlare";

        private const string affectedChannel = "Common";

        private float _endAfter = 0.0f;
        private bool _running = false;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActiveRadioComponent, RadioReceiveAttemptEvent>(OnRadioSendAttempt);
        }

        public override void Added()
        {
            base.Added();
            _endAfter = RobustRandom.Next(120, 240);
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
            if (_running && args.Channel.ID == affectedChannel && (HasComp<HeadsetComponent>(uid) || HasComp<HeadsetComponent>(args.Source)))
                args.Cancel();
        }

    }
}