using System.Linq;
using Content.Server.Speech.Components;
using Content.Server.Radio.Components;
using JetBrains.Annotations;

namespace Content.Server.Radio.EntitySystems
{
    [UsedImplicitly]
    public sealed class RadioSystem : EntitySystem
    {
        private readonly List<string> _messages = new();

        public void SpreadMessage(IRadio source, EntityUid speaker, string message, int channel)
        {
            if (_messages.Contains(message)) return;

            _messages.Add(message);
            string voiceID = GetSpeakerVoice(speaker);
            foreach (var radio in EntityManager.EntityQuery<IRadio>(true))
            {
                if (radio.Channels.Contains(channel))
                {
                    radio.Receive(message, channel, voiceID);
                }
            }

            _messages.Remove(message);
        }

        private string GetSpeakerVoice(EntityUid uid)
        {
            if (!TryComp<VoiceChangerVoiceComponent>(uid, out var voice))
            {
                return Comp<MetaDataComponent>(uid).EntityName;
            } else
            {
                return voice.VoiceName;
            }
        }
    }
}
