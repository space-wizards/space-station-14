using System.Collections.Generic;
using System.Linq;
using Content.Server.Radio.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Radio.EntitySystems
{
    [UsedImplicitly]
    public class RadioSystem : EntitySystem
    {
        private readonly List<string> _messages = new();

        public void SpreadMessage(IRadio source, IEntity speaker, string message, int channel)
        {
            if (_messages.Contains(message)) return;

            _messages.Add(message);

            foreach (var radio in EntityManager.EntityQuery<IRadio>(true))
            {
                if (radio.Channels.Contains(channel))
                {
                    //TODO: once voice identity gets added, pass into receiver via source.GetSpeakerVoice()
                    radio.Receive(message, channel, speaker);
                }
            }

            _messages.Remove(message);
        }
    }
}
