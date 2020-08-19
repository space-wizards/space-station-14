using Content.Server.GameObjects.Components.Interactable;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.MachineLinking
{
    [RegisterComponent]
    public class SignalTransmitterComponent : Component, IInteractUsing
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
#pragma warning restore 649

        public override string Name => "SignalTransmitter";

        private List<SignalReceiverComponent> _receivers;
        private float _range;

        public float Range { get => _range; private set => _range = value; }

        public override void Initialize()
        {
            base.Initialize();

            _receivers = new List<SignalReceiverComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _range, "range", 10);
        }

        public void TransmitSignal(SignalState state)
        {
            foreach (var receiver in _receivers)
            {
                receiver.DistributeSignal(state);
            }
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

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
                return false;

            if (tool.HasQuality(ToolQuality.Multitool)
                && eventArgs.Using.TryGetComponent<SignalLinkerComponent>(out var linker))
            {
                linker.Link = this;
                _notifyManager.PopupMessage(Owner, eventArgs.User, "Paired!");
            }

            return false;
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            foreach (var receiver in _receivers)
            {
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
