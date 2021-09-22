using System.Collections.Generic;
using System.Linq;
using Content.Server.Radio.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.Radio.EntitySystems
{
    /// <summary>
    ///     Handles broadcasting radio message events and keeping track of
    ///     listeners on each channel.
    /// </summary>
    [UsedImplicitly]
    public class RadioSystem : EntitySystem
    {
        /// <summary>
        ///     Stores the listening entities per channel.
        /// </summary>
        private readonly Dictionary<int, HashSet<EntityUid>> _cachedListeners = new();

        /// <summary>
        ///     Sends a new radio message over a channel from a source.
        /// </summary>
        public void BroadcastRadioMessage(EntityUid source, string message, int channel)
        {
            // no listeners on this channel, so who cares?
            if (!_cachedListeners.ContainsKey(channel))
                return;

            foreach (var listener in _cachedListeners[channel])
            {
                var msg = new RadioMessageEvent(message, channel, source);

                // Raised broadcast as well; some events, like ion storms, may wish to
                // alter every message that comes in on a channel, regardless of who is
                // listening.
                RaiseLocalEvent(listener, msg);
            }
        }

        /// <summary>
        ///     Rebroadcasts a radio message, first checking if the source has already
        ///     broadcasted it once before.
        /// </summary>
        public void RebroadcastRadioMessage(EntityUid source, RadioMessageEvent ev)
        {
            // Bad! No loops!
            if (ev.Sources.Contains(source))
                return;

            ev.Sources.Add(source);

            // we don't check for key because.. if this got broadcasted and rebroadcasted successfully,
            // then presumably the key already exists
            foreach (var listener in _cachedListeners[ev.Channel])
            {
                RaiseLocalEvent(listener, ev);
            }
        }

        /// <summary>
        ///     Adds a new listener to a channel.
        /// </summary>
        public void AddListener(EntityUid uid, int channel)
        {
            var set = _cachedListeners.GetOrNew(channel);
            set.Add(uid);
        }

        /// <summary>
        ///     Removes a listener from a channel.
        /// </summary>
        public void RemoveListener(EntityUid uid, int channel)
        {
            // why would you try to remove a listener on a channel that doesn't exist?
            if (!_cachedListeners.ContainsKey(channel))
                return;

            _cachedListeners[channel].Add(uid);
        }
    }

    public class RadioMessageEvent : EntityEventArgs
    {
        // funny tip: these can both be modified by any listener that's programmed to do so
        public string Message;
        public int Channel;

        /// <summary>
        ///     Sources is a hashset because a single message can be rebroadcast multiple times,
        ///     and we can use this hashset to determine all the listeners it passed through, either for
        ///     logging purposes, showing to chat, or just to prevent one source from broadcasting one
        ///     message in a loop.
        /// </summary>
        public HashSet<EntityUid> Sources;

        public RadioMessageEvent(string message, int channel, EntityUid source)
        {
            Message = message;
            Channel = channel;
            Sources = new HashSet<EntityUid>() { source };
        }

        public RadioMessageEvent(string message, int channel, HashSet<EntityUid> sources)
        {
            Message = message;
            Channel = channel;
            Sources = sources;
        }
    }
}
