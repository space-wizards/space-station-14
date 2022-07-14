using System.Linq;
using Content.Shared.Examine;
using Content.Server.Radio.Components;
using Content.Shared.Radio;
using JetBrains.Annotations;
using Content.Shared.Interaction;

namespace Content.Server.Radio.EntitySystems
{
    [UsedImplicitly]
    public sealed class RadioSystem : EntitySystem
    {
        private readonly List<string> _messages = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HandheldRadioComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<HandheldRadioComponent, ActivateInWorldEvent>(OnActivate);
        }

        private void OnActivate(EntityUid uid, HandheldRadioComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            component.Use(args.User);
        }

        private void OnExamine(EntityUid uid, HandheldRadioComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;
            args.PushMarkup(Loc.GetString("handheld-radio-component-on-examine",("frequency", component.BroadcastFrequency)));
        }

        public void SpreadMessage(IRadio source, EntityUid speaker, string message, RadioChannelPrototype channel)
        {
            if (_messages.Contains(message)) return;

            _messages.Add(message);

            foreach (var radio in EntityManager.EntityQuery<IRadio>(true))
            {
                //TODO: once voice identity gets added, pass into receiver via source.GetSpeakerVoice()
                radio.Receive(message, channel, speaker);
            }

            _messages.Remove(message);
        }
    }
}
