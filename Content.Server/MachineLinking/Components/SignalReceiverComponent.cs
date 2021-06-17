using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Content.Shared.Tool;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public class SignalReceiverComponent : Component, IInteractUsing
    {
        public override string Name => "SignalReceiver";

        private readonly List<SignalTransmitterComponent> _transmitters = new();

        [DataField("maxTransmitters")]
        private int? _maxTransmitters = default;

        public void DistributeSignal<T>(T state)
        {
            foreach (var comp in Owner.GetAllComponents<ISignalReceiver<T>>())
            {
                comp.TriggerSignal(state);
            }
        }

        public bool Subscribe(SignalTransmitterComponent transmitter)
        {
            if (_transmitters.Contains(transmitter))
            {
                return true;
            }

            if (_transmitters.Count >= _maxTransmitters) return false;

            transmitter.Subscribe(this);
            _transmitters.Add(transmitter);
            return true;
        }

        public void Unsubscribe(SignalTransmitterComponent transmitter)
        {
            transmitter.Unsubscribe(this);
            _transmitters.Remove(transmitter);
        }

        public void UnsubscribeAll()
        {
            for (var i = _transmitters.Count-1; i >= 0; i--)
            {
                var transmitter = _transmitters[i];
                if (transmitter.Deleted)
                {
                    continue;
                }

                transmitter.Unsubscribe(this);
            }
        }

        /// <summary>
        /// Subscribes/Unsubscribes a transmitter to this component. Returns whether it was successful.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="transmitter"></param>
        /// <returns></returns>
        public bool Interact(IEntity user, SignalTransmitterComponent? transmitter)
        {
            if (transmitter == null)
            {
                user.PopupMessage(Loc.GetString("Signal not set."));
                return false;
            }

            if (_transmitters.Contains(transmitter))
            {
                Unsubscribe(transmitter);
                Owner.PopupMessage(user, Loc.GetString("Unlinked."));
                return true;
            }

            if (transmitter.Range > 0 && !Owner.Transform.Coordinates.InRange(Owner.EntityManager, transmitter.Owner.Transform.Coordinates, transmitter.Range))
            {
                Owner.PopupMessage(user, Loc.GetString("Out of range."));
                return false;
            }

            if (!Subscribe(transmitter))
            {
                Owner.PopupMessage(user, Loc.GetString("Max Transmitters reached!"));
                return false;
            }
            Owner.PopupMessage(user, Loc.GetString("Linked!"));
            return true;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
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

            UnsubscribeAll();

            _transmitters.Clear();
        }
    }
}
