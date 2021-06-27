using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Content.Shared.Tool;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public class SignalTransmitterComponent : Component, IInteractUsing, ISerializationHooks
    {
        public override string Name => "SignalTransmitter";

        private List<SignalReceiverComponent>? _unresolvedReceivers = new();
        private List<SignalReceiverComponent> _receivers = new();

        /// <summary>
        /// 0 is unlimited range
        /// </summary>
        [ViewVariables]
        [DataField("range")]
        public float Range { get; private set; } = 10;

        [DataField("signalReceivers")] private List<EntityUid> _receiverIds = new();

        void ISerializationHooks.BeforeSerialization()
        {
            var entityList = new List<EntityUid>();

            foreach (var receiver in _receivers)
            {
                if (receiver.Deleted)
                {
                    continue;
                }

                entityList.Add(receiver.Owner.Uid);
            }

            _receiverIds = entityList;
        }

        void ISerializationHooks.AfterDeserialization()
        {
            _unresolvedReceivers = new List<SignalReceiverComponent>();

            foreach (var id in _receiverIds)
            {
                if (!Owner.EntityManager.TryGetEntity(id, out var entity) ||
                    !entity.TryGetComponent<SignalReceiverComponent>(out var receiver))
                {
                    continue;
                }

                _unresolvedReceivers.Add(receiver);
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            _receivers = new List<SignalReceiverComponent>();

            if (_unresolvedReceivers != null)
            {
                foreach (var receiver in _unresolvedReceivers)
                {
                    receiver.Subscribe(this);
                }

                _unresolvedReceivers = null;
            }
        }

        public bool TransmitSignal<T>(T signal)
        {
            if (_receivers.Count == 0)
            {
                return false;
            }

            foreach (var receiver in _receivers)
            {
                if (Range > 0 && !Owner.Transform.Coordinates.InRange(Owner.EntityManager, receiver.Owner.Transform.Coordinates, Range))
                {
                    continue;
                }

                receiver.DistributeSignal(signal);
            }
            return true;
        }

        public void Subscribe(SignalReceiverComponent receiver)
        {
            if (_receivers.Contains(receiver))
            {
                return;
            }

            _receivers.Add(receiver);
        }

        public void Unsubscribe(SignalReceiverComponent receiver)
        {
            _receivers.Remove(receiver);
        }

        public SignalTransmitterComponent GetSignal(IEntity? user)
        {
            if (user != null)
            {
                Owner.PopupMessage(user, Loc.GetString("signal-transmitter-component-get-signal-success"));
            }

            return this;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
                return false;

            if (tool.HasQuality(ToolQuality.Multitool)
                && eventArgs.Using.TryGetComponent<SignalLinkerComponent>(out var linker))
            {
                linker.Link = GetSignal(eventArgs.User);
            }

            return false;
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            for (var i = _receivers.Count-1; i >= 0; i++)
            {
                var receiver = _receivers[i];
                if (receiver.Deleted)
                {
                    continue;
                }

                receiver.Unsubscribe(this);
            }

            _receivers.Clear();
        }
    }
}
