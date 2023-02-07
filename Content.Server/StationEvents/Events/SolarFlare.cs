using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Radio.Components;
using Content.Server.Radio;
using Robust.Shared.Random;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Components;

namespace Content.Server.StationEvents.Events
{
    public sealed class SolarFlare : StationEventSystem
    {
        [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Prototype => "SolarFlare";

        private bool _onlyJamHeadsets = true;
        private HashSet<string> _affectedChannels = new();
        private float _endAfter = 0.0f;
        private bool _running = false;
        private float _lightBurnChance = 0.0f;
        private float _lightChangeColorChance = 0.0f;

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
            _endAfter = _random.Next(ev.MinEndAfter, ev.MaxEndAfter);
            _affectedChannels = ev.AffectedChannels;
            _lightBurnChance = ev.LightBreakChance;
        }

        public override void Started()
        {
            base.Started();
            _running = true;
            MessLights();
        }

        private void MessLights() 
        {
            foreach (var comp in EntityQuery<PoweredLightComponent>()) 
            {
                if (_random.Prob(_lightBurnChance))
                {
                    _poweredLight.TryDestroyBulb(comp.Owner, comp);
                }
            }
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