#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public sealed class DoAfterComponent : SharedDoAfterComponent
    {
        public IReadOnlyCollection<DoAfter> DoAfters => _doAfters.Keys;
        private readonly Dictionary<DoAfter, byte> _doAfters = new Dictionary<DoAfter, byte>();

        // So the client knows which one to update (and so we don't send all of the do_afters every time 1 updates)
        // we'll just send them the index. Doesn't matter if it wraps around.
        private byte _runningIndex;

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PlayerAttachedMsg _:
                    UpdateClient();
                    break;
            }
        }

        // Only sending data to the relevant client (at least, other clients don't need to know about do_after for now).
        private void UpdateClient()
        {
            if (!TryGetConnectedClient(out var connectedClient))
            {
                return;
            }

            foreach (var (doAfter, id) in _doAfters)
            {
                // THE ALMIGHTY PYRAMID
                var message = new DoAfterMessage(
                    id,
                    doAfter.UserGrid,
                    doAfter.TargetGrid,
                    doAfter.StartTime,
                    doAfter.EventArgs.Delay,
                    doAfter.EventArgs.BreakOnUserMove,
                    doAfter.EventArgs.BreakOnTargetMove,
                    doAfter.EventArgs.Target?.Uid ?? EntityUid.Invalid);

                SendNetworkMessage(message, connectedClient);
            }
        }

        private bool TryGetConnectedClient(out INetChannel? connectedClient)
        {
            connectedClient = null;

            if (!Owner.TryGetComponent(out IActorComponent? actorComponent))
            {
                return false;
            }

            connectedClient = actorComponent.playerSession.ConnectedClient;
            if (!connectedClient.IsConnected)
            {
                return false;
            }

            return true;
        }

        public void Add(DoAfter doAfter)
        {
            _doAfters.Add(doAfter, _runningIndex);

            if (TryGetConnectedClient(out var connectedClient))
            {
                var message = new DoAfterMessage(
                    _runningIndex,
                    doAfter.UserGrid,
                    doAfter.TargetGrid,
                    doAfter.StartTime,
                    doAfter.EventArgs.Delay,
                    doAfter.EventArgs.BreakOnUserMove,
                    doAfter.EventArgs.BreakOnTargetMove,
                    doAfter.EventArgs.Target?.Uid ?? EntityUid.Invalid);

                SendNetworkMessage(message, connectedClient);
            }

            _runningIndex++;
        }

        public void Cancelled(DoAfter doAfter)
        {
            if (!_doAfters.TryGetValue(doAfter, out var index))
            {
                return;
            }

            if (TryGetConnectedClient(out var connectedClient))
            {
                var message = new CancelledDoAfterMessage(index);
                SendNetworkMessage(message, connectedClient);
            }

            _doAfters.Remove(doAfter);
        }

        /// <summary>
        ///     Call when the particular DoAfter is finished.
        ///     Client should be tracking this independently.
        /// </summary>
        /// <param name="doAfter"></param>
        public void Finished(DoAfter doAfter)
        {
            if (!_doAfters.ContainsKey(doAfter))
            {
                return;
            }

            _doAfters.Remove(doAfter);
        }
    }
}
