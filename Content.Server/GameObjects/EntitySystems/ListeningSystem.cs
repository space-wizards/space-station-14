using Content.Server.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.EntitySystems
{
    class ListeningSystem : EntitySystem
    {
        private readonly List<ListeningComponent> _activeListeners = new List<ListeningComponent>();

        public void Subscribe(ListeningComponent listener)
        {
            if (_activeListeners.Contains(listener))
            {
                return;
            }

            _activeListeners.Add(listener);

            return;
        }

        public void Unsubscribe(ListeningComponent listener)
        {
            if (!_activeListeners.Contains(listener))
            {
                return;
            }

            _activeListeners.Remove(listener);

            return;
        }

        public List<ListeningComponent> GetActiveListeners()
        {
            return _activeListeners;
        }
    }
}
