using System;
using System.Collections.Generic;
using System.Linq;
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

        public IEntity ProviderOwner { get; }
    }

    [RegisterComponent]
    public class PowerProviderComponent : BaseApcNetComponent, IPowerProvider
    {
        public override string Name => "PowerProvider";

        public IEntity ProviderOwner => Owner;

        /// <summary>
        ///     The max distance this can transmit power to <see cref="PowerReceiverComponent"/>s from.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int PowerTransferRange { get => _powerTransferRange; set => SetPowerTransferRange(value); }
        private int _powerTransferRange;

        [ViewVariables]
        public IReadOnlyList<PowerReceiverComponent> LinkedReceivers => _linkedReceivers;
        private List<PowerReceiverComponent> _linkedReceivers = new List<PowerReceiverComponent>();

        /// <summary>
        ///     If <see cref="PowerReceiverComponent"/>s should consider connecting to this.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Connectable { get; private set; } = true;

        public static readonly IPowerProvider NullProvider = new NullPowerProvider();

        public void AddReceiver(PowerReceiverComponent receiver)
        {
            _linkedReceivers.Add(receiver);
            Net.UpdatePowerProviderReceivers(this);
        }

        public void RemoveReceiver(PowerReceiverComponent receiver)
        {
            _linkedReceivers.Remove(receiver);
            Net.UpdatePowerProviderReceivers(this);
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
            _linkedReceivers = new List<PowerReceiverComponent>();
            Net.UpdatePowerProviderReceivers(this);
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
            return nearbyEntities.Select(entity => entity.TryGetComponent<PowerReceiverComponent>(out var receiver) ? receiver : null)
                .Where(receiver => receiver != null)
                .Where(receiver => receiver.Connectable)
                .Where(receiver => receiver.NeedsProvider)
                .Where(receiver => receiver.Owner.Transform.Coordinates.TryDistance(Owner.EntityManager, Owner.Transform.Coordinates, out var distance) && distance < Math.Min(PowerTransferRange, receiver.PowerReceptionRange))
                .ToList();
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
            foreach (var receiver in _linkedReceivers.ToArray())
            {
                receiver.ClearProvider();
            }
            _powerTransferRange = newPowerTransferRange;
            _linkedReceivers = FindAvailableReceivers();
            Net.UpdatePowerProviderReceivers(this);
        }

        private class NullPowerProvider : IPowerProvider
        {
            public void AddReceiver(PowerReceiverComponent receiver) { }
            public void RemoveReceiver(PowerReceiverComponent receiver) { }
            public IEntity ProviderOwner => default;
        }
    }
}
