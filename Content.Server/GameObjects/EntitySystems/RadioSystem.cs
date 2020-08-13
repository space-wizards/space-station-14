using System.Collections.Generic;
using Content.Server.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    internal sealed class RadioSystem : EntitySystem
    {
        private readonly List<string> _messages = new List<string>();

        public void SpreadMessage(IEntity source, string message)
        {
            if (_messages.Contains(message))
            {
                return;
            }

            _messages.Add(message);

            foreach (var radio in ComponentManager.EntityQuery<RadioComponent>())
            {
                if (radio.Owner == source || !radio.RadioOn)
                {
                    continue;
                }

                radio.Speaker(message);
            }

            _messages.Remove(message);
        }
    }
}
