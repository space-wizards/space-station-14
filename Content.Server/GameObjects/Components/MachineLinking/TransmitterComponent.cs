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
    public class TransmitterComponent : Component, IInteractUsing
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
#pragma warning restore 649

        public override string Name => "Transmitter";

        private List<ReceiverComponent> _receivers;
        private float _range;

        public float Range { get => _range; private set => _range = value; }

        public override void Initialize()
        {
            base.Initialize();

            _receivers = new List<ReceiverComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _range, "range", 10);
        }

        public void TransmitTrigger(bool state)
        {
            foreach (var receiver in _receivers)
            {
                receiver.DistributeTrigger(state);
            }
        }

        public void Subscribe(ReceiverComponent receiver)
        {
            _receivers.Add(receiver);
        }

        public void Unsubscribe(ReceiverComponent receiver)
        {
            _receivers.Remove(receiver);
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
                return false;

            if (tool.HasQuality(ToolQuality.Multitool)
                && eventArgs.Using.TryGetComponent<LinkerComponent>(out var linker))
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
