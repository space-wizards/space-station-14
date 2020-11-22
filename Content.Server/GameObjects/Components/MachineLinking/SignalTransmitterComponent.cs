using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.MachineLinking
{
    [RegisterComponent]
    public class SignalTransmitterComponent : Component, IInteractUsing
    {
        public override string Name => "SignalTransmitter";

        private List<SignalReceiverComponent> _unresolvedReceivers;
        private List<SignalReceiverComponent> _receivers;
        [ViewVariables] private float _range;

        /// <summary>
        /// 0 is unlimited range
        /// </summary>
        public float Range { get => _range; private set => _range = value; }

        public override void Initialize()
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _range, "range", 10);
            if (serializer.Reading)
            {
                if (!serializer.TryReadDataField("signalReceivers", out List<EntityUid> entityUids))
                {
                    return;
                }

                _unresolvedReceivers = new List<SignalReceiverComponent>();
                foreach (var entityUid in entityUids)
                {
                    if (!Owner.EntityManager.TryGetEntity(entityUid, out var entity)
                        || !entity.TryGetComponent<SignalReceiverComponent>(out var receiver))
                    {
                        continue;
                    }

                    _unresolvedReceivers.Add(receiver);
                }
            }
            else if (serializer.Writing)
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

                serializer.DataWriteFunction("signalReceivers", null, () => entityList);
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

        public SignalTransmitterComponent GetSignal(IEntity user)
        {
            if (user != null)
            {
                Owner.PopupMessage(user, Loc.GetString("Signal fetched."));
            }

            return this;
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
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
