using Content.Server.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    class RadioSystem : EntitySystem
    {
        private List<string> _messages;

        public override void Initialize()
        {
            base.Initialize();

            _messages = new List<string>();
        }

        public void SpreadMessage(IRadio source, IEntity speaker, string message, int channel)
        {
            if (_messages.Contains(message)) return;

            _messages.Add(message);

            foreach (var radio in ComponentManager.EntityQuery<IRadio>())
            {
                if (radio != source && radio.Channels.Contains(channel))
                {
                    //TODO: once voice identity gets added, pass into receiver via source.GetSpeakerVoice()
                    radio.Receiver(message, channel, speaker);
                }
            }

            _messages.Remove(message);
        }
    }
}
