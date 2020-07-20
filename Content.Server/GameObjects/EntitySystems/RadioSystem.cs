using Content.Server.GameObjects.Components.Interactable;
using Robust.Shared.GameObjects.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Content.Server.GameObjects.EntitySystems
{
    class RadioSystem : EntitySystem
    {
        private readonly HashSet<RadioComponent> _activeRadios = new HashSet<RadioComponent>();

        public bool Subscribe(RadioComponent radio)
        {
            return _activeRadios.Add(radio);
        }

        public bool Unsubscribe(RadioComponent radio)
        {
            return _activeRadios.Remove(radio);
        }

        public void SpreadMessage(RadioComponent source, string message)
        {
            foreach (RadioComponent radio in _activeRadios.ToArray())
            {
                if (radio == source || radio == null) { continue; }
                radio.Speaker(message);
            }
        }
    }
}
