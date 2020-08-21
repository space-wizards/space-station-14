#nullable enable
using System;
using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public sealed class DoAfterComponent : SharedDoAfterComponent
    {
        public override string Name => "DoAfter";

        public IReadOnlyDictionary<byte, DoAfterMessage> DoAfters => _doAfters;
        private readonly Dictionary<byte, DoAfterMessage> _doAfters = new Dictionary<byte, DoAfterMessage>();
        
        public readonly List<(TimeSpan CancelTime, DoAfterMessage Message)> CancelledDoAfters = 
                     new List<(TimeSpan CancelTime, DoAfterMessage Message)>();

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);
            switch (message)
            {
                case DoAfterMessage msg:
                    _doAfters.Add(msg.ID, msg);
                    EntitySystem.Get<DoAfterSystem>().Gui?.AddDoAfter(msg);
                    break;
                case CancelledDoAfterMessage msg:
                    Cancel(msg.ID);
                    break;
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PlayerDetachedMsg _:
                    _doAfters.Clear();
                    CancelledDoAfters.Clear();
                    break;
            }
        }

        /// <summary>
        ///     Remove a DoAfter without showing a cancellation graphic.
        /// </summary>
        /// <param name="doAfter"></param>
        public void Remove(DoAfterMessage doAfter)
        {
            if (_doAfters.ContainsKey(doAfter.ID))
            {
                _doAfters.Remove(doAfter.ID);
            }

            for (var i = CancelledDoAfters.Count - 1; i >= 0; i--)
            {
                var cancelled = CancelledDoAfters[i];

                if (cancelled.Message == doAfter)
                {
                    CancelledDoAfters.RemoveAt(i);
                    break;
                }
            }
            
            EntitySystem.Get<DoAfterSystem>().Gui?.RemoveDoAfter(doAfter.ID);
        }

        /// <summary>
        ///     Mark a DoAfter as cancelled and show a cancellation graphic.
        /// </summary>
        ///     Actual removal is handled by DoAfterEntitySystem.
        /// <param name="id"></param>
        /// <param name="currentTime"></param>
        public void Cancel(byte id, TimeSpan? currentTime = null)
        {
            foreach (var (_, cancelled) in CancelledDoAfters)
            {
                if (cancelled.ID == id)
                {
                    return;
                }
            }

            var doAfterMessage = _doAfters[id];
            currentTime ??= IoCManager.Resolve<IGameTiming>().CurTime;
            CancelledDoAfters.Add((currentTime.Value, doAfterMessage));
            EntitySystem.Get<DoAfterSystem>().Gui?.CancelDoAfter(id);
        }
    }
}