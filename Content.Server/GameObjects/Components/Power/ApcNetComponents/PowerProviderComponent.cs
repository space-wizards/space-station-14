using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NewPower.ApcNetComponents
{
    /// <summary>
    ///     Relays <see cref="PowerReceiverComponent"/>s in an area to a <see cref="IApcNet"/> so they can receive power.
    /// </summary>
    public interface IPowerProvider
    {
        void AddReceiver(PowerReceiverComponent receiver);

        void RemoveReceiver(PowerReceiverComponent receiver);
    }

    [RegisterComponent]
    public class PowerProviderComponent : BaseApcNetComponent, IPowerProvider
    {
        public override string Name => "NewPowerProvider";

        [ViewVariables(VVAccess.ReadWrite)]
        public int PowerTransferRange { get => _powerTransferRange; set => SetPowerTransferRange(value); }
        private int _powerTransferRange;

        [ViewVariables]
        public IReadOnlyList<PowerReceiverComponent> LinkedReceivers => _linkedReceivers;
        private List<PowerReceiverComponent> _linkedReceivers = new List<PowerReceiverComponent>();

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

        public override void Initialize()
        {
            base.Initialize();
            _linkedReceivers = FindAvailableReceivers();
        }

        private List<PowerReceiverComponent> FindAvailableReceivers()
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            return IoCManager.Resolve<IServerEntityManager>()
                .GetEntitiesInRange(Owner.Transform.GridPosition, PowerTransferRange)
                .Select(entity => entity.TryGetComponent<PowerReceiverComponent>(out var receiver) ? receiver : null)
                .Where(receiver => receiver != null)
                .Where(receiver => receiver.NeedsProvider)
                .Where(receiver => receiver.Owner.Transform.GridPosition.Distance(mapManager, Owner.Transform.GridPosition) < Math.Min(PowerTransferRange, receiver.PowerReceptionRange))
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
            _powerTransferRange = newPowerTransferRange;
            _linkedReceivers = FindAvailableReceivers();
            Net.UpdatePowerProviderReceivers(this);
        }

        private class NullPowerProvider : IPowerProvider
        {
            public void AddReceiver(PowerReceiverComponent receiver) { }
            public void RemoveReceiver(PowerReceiverComponent receiver) { }
        }
    }
}
