using System;
using System.Collections.Generic;
using System.Diagnostics;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents
{
    /// <summary>
    ///     Relays <see cref="PowerReceiverComponent"/>s in an area to a <see cref="IApcNet"/> so they can receive power.
    /// </summary>
    public interface IPowerProvider
    {
        void AddReceiver(PowerReceiverComponent receiver);

        void RemoveReceiver(PowerReceiverComponent receiver);

        void UpdateReceiverLoad(int oldLoad, int newLoad);

        public IEntity ProviderOwner { get; }

        public bool HasApcPower { get; }
    }

    [RegisterComponent]
    public class PowerProviderComponent : BaseApcNetComponent, IPowerProvider
    {
        public override string Name => "PowerProvider";

        public IEntity ProviderOwner => Owner;

        [ViewVariables]
        public bool HasApcPower => Net.Powered;

        /// <summary>
        ///     The max distance this can transmit power to <see cref="PowerReceiverComponent"/>s from.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int PowerTransferRange { get => _powerTransferRange; set => SetPowerTransferRange(value); }
        private int _powerTransferRange;

        [ViewVariables]
        public IReadOnlyList<PowerReceiverComponent> LinkedReceivers => _linkedReceivers;
        private List<PowerReceiverComponent> _linkedReceivers = new();

        /// <summary>
        ///     If <see cref="PowerReceiverComponent"/>s should consider connecting to this.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Connectable { get; private set; } = true;

        public static readonly IPowerProvider NullProvider = new NullPowerProvider();

        public void AddReceiver(PowerReceiverComponent receiver)
        {
            var oldLoad = GetTotalLoad();
            _linkedReceivers.Add(receiver);
            var newLoad = oldLoad + receiver.Load;
            Net.UpdatePowerProviderReceivers(this, oldLoad, newLoad);
        }

        public void RemoveReceiver(PowerReceiverComponent receiver)
        {
            var oldLoad = GetTotalLoad();
            _linkedReceivers.Remove(receiver);
            var newLoad = oldLoad - receiver.Load;
            Net.UpdatePowerProviderReceivers(this, oldLoad, newLoad);
        }

        public void UpdateReceiverLoad(int oldLoad, int newLoad)
        {
            Net.UpdatePowerProviderReceivers(this, oldLoad, newLoad);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _powerTransferRange, "powerTransferRange", 3);
        }

        protected override void Startup()
        {
            base.Startup();
            foreach (var receiver in FindAvailableReceivers())
            {
                receiver.Provider = this;
            }
        }

        public override void OnRemove()
        {
            Connectable = false;
            var receivers = _linkedReceivers.ToArray();
            foreach (var receiver in receivers)
            {
                receiver.ClearProvider();
            }
            foreach (var receiver in receivers)
            {
                receiver.TryFindAndSetProvider();
            }
            base.OnRemove();
        }

        private List<PowerReceiverComponent> FindAvailableReceivers()
        {
            var nearbyEntities = Owner.EntityManager
                .GetEntitiesInRange(Owner, PowerTransferRange);

            var receivers = new List<PowerReceiverComponent>();

            foreach (var entity in nearbyEntities)
            {
                if (entity.TryGetComponent<PowerReceiverComponent>(out var receiver) &&
                    receiver.Connectable &&
                    receiver.NeedsProvider &&
                    receiver.Owner.Transform.Coordinates.TryDistance(Owner.EntityManager, Owner.Transform.Coordinates, out var distance) &&
                    distance < Math.Min(PowerTransferRange, receiver.PowerReceptionRange))
                {
                    receivers.Add(receiver);
                }
            }
            return receivers;
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
            var receivers = _linkedReceivers.ToArray();

            foreach (var receiver in receivers)
            {
                receiver.ClearProvider();
            }
            _powerTransferRange = newPowerTransferRange;

            foreach (var receiver in receivers)
            {
                receiver.TryFindAndSetProvider();
            }
        }

        private int GetTotalLoad()
        {
            var load = 0;
            foreach (var receiver in _linkedReceivers)
            {
                load += receiver.Load;
            }
            return load;
        }

        private class NullPowerProvider : IPowerProvider
        {
            /// <summary>
            ///     It is important that this returns false, so <see cref="PowerReceiverComponent"/>s with a <see cref="NullPowerProvider"/> have no power.
            /// </summary>
            public bool HasApcPower => false;

            public void AddReceiver(PowerReceiverComponent receiver) { }
            public void RemoveReceiver(PowerReceiverComponent receiver) { }
            public void UpdateReceiverLoad(int oldLoad, int newLoad) { }
            public IEntity ProviderOwner => default;
        }
    }
}
