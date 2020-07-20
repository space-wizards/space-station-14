using Content.Server.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.EntitySystems
{
    class ListeningSystem : EntitySystem
    {
        private readonly HashSet<ListeningComponent> _activeListeners = new HashSet<ListeningComponent>();

        public bool Subscribe(ListeningComponent listener)
        {
            return _activeListeners.Add(listener);
        }

        public bool Unsubscribe(ListeningComponent listener)
        {
            return _activeListeners.Remove(listener);
        }

        public HashSet<ListeningComponent> GetActiveListeners()
        {
            return _activeListeners;
        }
    }
}
