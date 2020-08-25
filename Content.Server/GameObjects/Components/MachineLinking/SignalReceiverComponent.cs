using Content.Server.GameObjects.Components.Interactable;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.MachineLinking
{
    [RegisterComponent]
    public class SignalReceiverComponent : Component, IInteractUsing
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public override string Name => "SignalReceiver";

        private List<SignalTransmitterComponent> _transmitters;

        public override void Initialize()
        {
            base.Initialize();

            _transmitters = new List<SignalTransmitterComponent>();
        }

        public void DistributeSignal(SignalState state)
        {
            foreach (var comp in Owner.GetAllComponents<ISignalReceiver>())
            {
                comp.TriggerSignal(state);
            }
        }

        public void Subscribe(SignalTransmitterComponent transmitter)
        {
            if (_transmitters.Contains(transmitter))
            {
                return;
            }

            transmitter.Subscribe(this);
            _transmitters.Add(transmitter);
        }

        public void Unsubscribe(SignalTransmitterComponent transmitter)
        {
            transmitter.Unsubscribe(this);
            _transmitters.Remove(transmitter);
        }

        /// <summary>
        /// Subscribes/Unsubscribes a transmitter to this component. Returns whether it was successful.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="transmitter"></param>
        /// <returns></returns>
        public bool Interact(IEntity user, SignalTransmitterComponent transmitter)
        {
            if (transmitter == null)
            {
                _notifyManager.PopupMessage(user, user, Loc.GetString("Signal not set."));
                return false;
            }

            if (_transmitters.Contains(transmitter))
            {
                Unsubscribe(transmitter);
                _notifyManager.PopupMessage(Owner, user, Loc.GetString("Unlinked."));
                return true;
            }

            if (transmitter.Range > 0 && !Owner.Transform.GridPosition.InRange(_mapManager, transmitter.Owner.Transform.GridPosition, transmitter.Range))
            {
                _notifyManager.PopupMessage(Owner, user, Loc.GetString("Out of range."));
                return false;
            }

            Subscribe(transmitter);
            _notifyManager.PopupMessage(Owner, user, Loc.GetString("Linked!"));
            return true;
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
                return false;

            if (tool.HasQuality(ToolQuality.Multitool)
                && eventArgs.Using.TryGetComponent<SignalLinkerComponent>(out var linker))
            {
                return Interact(eventArgs.User, linker.Link);
            }

            return false;
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            foreach (var transmitter in _transmitters)
            {
                if (transmitter.Deleted)
                {
                    continue;
                }

                transmitter.Unsubscribe(this);
            }
            _transmitters.Clear();
        }
    }
}
