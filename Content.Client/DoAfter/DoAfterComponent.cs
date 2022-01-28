using System;
using System.Collections.Generic;
using Content.Client.DoAfter.UI;
using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Client.DoAfter
{
    [RegisterComponent]
    public sealed class DoAfterComponent : SharedDoAfterComponent
    {
        public override string Name => "DoAfter";

        public IReadOnlyDictionary<byte, ClientDoAfter> DoAfters => _doAfters;
        private readonly Dictionary<byte, ClientDoAfter> _doAfters = new();

        public readonly List<(TimeSpan CancelTime, ClientDoAfter Message)> CancelledDoAfters = new();

        public DoAfterGui? Gui { get; set; }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);
            switch (message)
            {
                case CancelledDoAfterMessage msg:
                    Cancel(msg.ID);
                    break;
            }
        }

        protected override void OnAdd()
        {
            base.OnAdd();
            Enable();
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            Disable();
        }

        /// <summary>
        ///     For handling PVS so we dispose of controls if they go out of range
        /// </summary>
        public void Enable()
        {
            if (Gui?.Disposed == false)
                return;

            Gui = new DoAfterGui {AttachedEntity = Owner};

            foreach (var (_, doAfter) in _doAfters)
            {
                Gui.AddDoAfter(doAfter);
            }

            foreach (var (_, cancelled) in CancelledDoAfters)
            {
                Gui.CancelDoAfter(cancelled.ID);
            }
        }

        public void Disable()
        {
            Gui?.Dispose();
            Gui = null;
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not DoAfterComponentState state)
                return;

            var toRemove = new List<ClientDoAfter>();

            foreach (var (id, doAfter) in _doAfters)
            {
                var found = false;

                foreach (var clientdoAfter in state.DoAfters)
                {
                    if (clientdoAfter.ID == id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    toRemove.Add(doAfter);
                }
            }

            foreach (var doAfter in toRemove)
            {
                Remove(doAfter);
            }

            foreach (var doAfter in state.DoAfters)
            {
                if (_doAfters.ContainsKey(doAfter.ID))
                    continue;

                _doAfters.Add(doAfter.ID, doAfter);
            }

            if (Gui == null || Gui.Disposed)
                return;

            foreach (var (_, doAfter) in _doAfters)
            {
                Gui.AddDoAfter(doAfter);
            }
        }

        /// <summary>
        ///     Remove a DoAfter without showing a cancellation graphic.
        /// </summary>
        /// <param name="clientDoAfter"></param>
        public void Remove(ClientDoAfter clientDoAfter)
        {
            _doAfters.Remove(clientDoAfter.ID);

            var found = false;

            for (var i = CancelledDoAfters.Count - 1; i >= 0; i--)
            {
                var cancelled = CancelledDoAfters[i];

                if (cancelled.Message == clientDoAfter)
                {
                    CancelledDoAfters.RemoveAt(i);
                    found = true;
                    break;
                }
            }

            if (!found)
                _doAfters.Remove(clientDoAfter.ID);

            Gui?.RemoveDoAfter(clientDoAfter.ID);
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
                    return;
            }

            if (!_doAfters.ContainsKey(id))
                return;

            var doAfterMessage = _doAfters[id];
            currentTime ??= IoCManager.Resolve<IGameTiming>().CurTime;
            CancelledDoAfters.Add((currentTime.Value, doAfterMessage));
            Gui?.CancelDoAfter(id);
        }
    }
}
