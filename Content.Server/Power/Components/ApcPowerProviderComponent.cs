#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.Power.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public class ApcPowerProviderComponent : BaseApcNetComponent
    {
        public override string Name => "PowerProvider";

        public IEntity ProviderOwner => Owner;

        /// <summary>
        ///     The max distance this can transmit power to <see cref="ApcPowerReceiverComponent"/>s from.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int PowerTransferRange { get => _powerTransferRange; set => SetPowerTransferRange(value); }
        [DataField("powerTransferRange")]
        private int _powerTransferRange = 3;

        [ViewVariables] public List<ApcPowerReceiverComponent> LinkedReceivers { get; } = new();

        /// <summary>
        ///     If <see cref="ApcPowerReceiverComponent"/>s should consider connecting to this.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Connectable { get; private set; } = true;

        public void AddReceiver(ApcPowerReceiverComponent receiver)
        {
            LinkedReceivers.Add(receiver);
            receiver.NetworkLoad.LinkedNetwork = default;

            Net?.QueueNetworkReconnect();
        }

        public void RemoveReceiver(ApcPowerReceiverComponent receiver)
        {
            LinkedReceivers.Remove(receiver);
            receiver.NetworkLoad.LinkedNetwork = default;

            Net?.QueueNetworkReconnect();
        }

        protected override void Startup()
        {
            base.Startup();

            foreach (var receiver in FindAvailableReceivers())
            {
                receiver.Provider = this;
            }
        }

        protected override void OnRemove()
        {
            Connectable = false;
            var receivers = LinkedReceivers.ToArray();
            foreach (var receiver in receivers)
            {
                receiver.Provider = null;
            }
            foreach (var receiver in receivers)
            {
                receiver.TryFindAndSetProvider();
            }
            base.OnRemove();
        }

        private IEnumerable<ApcPowerReceiverComponent> FindAvailableReceivers()
        {
            var nearbyEntities = IoCManager.Resolve<IEntityLookup>()
                .GetEntitiesInRange(Owner, PowerTransferRange);

            foreach (var entity in nearbyEntities)
            {
                if (entity.TryGetComponent<ApcPowerReceiverComponent>(out var receiver) &&
                    receiver.Connectable &&
                    receiver.NeedsProvider &&
                    receiver.Owner.Transform.Coordinates.TryDistance(Owner.EntityManager, Owner.Transform.Coordinates, out var distance) &&
                    distance < Math.Min(PowerTransferRange, receiver.PowerReceptionRange))
                {
                    yield return receiver;
                }
            }
        }

        protected override void AddSelfToNet(IApcNet apcNet)
        {
            apcNet.AddPowerProvider(this);
        }

        protected override void RemoveSelfFromNet(IApcNet apcNet)
        {
            apcNet.RemovePowerProvider(this);
        }

        private void SetPowerTransferRange(int newPowerTransferRange)
        {
            var receivers = LinkedReceivers.ToArray();

            foreach (var receiver in receivers)
            {
                receiver.Provider = null;
            }

            _powerTransferRange = newPowerTransferRange;

            foreach (var receiver in receivers)
            {
                receiver.TryFindAndSetProvider();
            }
        }
    }
}
