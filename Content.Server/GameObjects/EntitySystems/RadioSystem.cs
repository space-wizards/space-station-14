using Content.Server.GameObjects.Components.Interactable;
using Content.Server.Interfaces;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    internal sealed class RadioSystem : EntitySystem
    {
        private readonly List<string> _messages = new List<string>();

        public override void Initialize()
        {
            base.Initialize();

            _messages = new List<string>();
        }

        public void SpreadMessage(IRadio source, IEntity speaker, string message, int channel)
        {
            if (_messages.Contains(message)) { return; }

            _messages.Add(message);

            foreach (var radio in ComponentManager.EntityQuery<IRadio>())
            {
                if (radio != source && radio.GetChannels().Contains(channel))
                {
                    //TODO: once voice identity gets added, pass into receiver via source.GetSpeakerVoice()
                    radio.Receiver(message, channel, speaker);
                }
            }

            _messages.Remove(message);
        }
    }
}
