using System.Linq;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Pow3r;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Power.NodeGroups
{
    public interface IApcNet : IBasePowerNet
    {
        void AddApc(ApcComponent apc);

        void RemoveApc(ApcComponent apc);

        void AddPowerProvider(ApcPowerProviderComponent provider);

        void RemovePowerProvider(ApcPowerProviderComponent provider);

        void QueueNetworkReconnect();
    }

    [NodeGroup(NodeGroupID.Apc)]
    [UsedImplicitly]
    public sealed class ApcNet : BaseNetConnectorNodeGroup<IApcNet>, IApcNet
    {
        private PowerNetSystem? _powerNetSystem;

        [ViewVariables] public readonly List<ApcComponent> Apcs = new();
        [ViewVariables] public readonly List<ApcPowerProviderComponent> Providers = new();
        [ViewVariables] public readonly List<PowerConsumerComponent> Consumers = new();

        //Debug property
        [ViewVariables] private int TotalReceivers => Providers.Sum(provider => provider.LinkedReceivers.Count);

        [ViewVariables]
        private IEnumerable<ApcPowerReceiverComponent> AllReceivers =>
            Providers.SelectMany(provider => provider.LinkedReceivers);

        [ViewVariables]
        public PowerState.Network NetworkNode { get; } = new();

        public override void Initialize(Node sourceNode, IEntityManager entMan)
        {
            base.Initialize(sourceNode, entMan);

            _powerNetSystem = entMan.EntitySysManager.GetEntitySystem<PowerNetSystem>();
            _powerNetSystem.InitApcNet(this);
        }

        public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
        {
            base.AfterRemake(newGroups);

            _powerNetSystem?.DestroyApcNet(this);
        }

        public void AddApc(ApcComponent apc)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(apc.Owner, out PowerNetworkBatteryComponent? netBattery))
                netBattery.NetworkBattery.LinkedNetworkDischarging = default;

            QueueNetworkReconnect();
            Apcs.Add(apc);
        }

        public void RemoveApc(ApcComponent apc)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(apc.Owner, out PowerNetworkBatteryComponent? netBattery))
                netBattery.NetworkBattery.LinkedNetworkDischarging = default;

            QueueNetworkReconnect();
            Apcs.Remove(apc);
        }

        public void AddPowerProvider(ApcPowerProviderComponent provider)
        {
            Providers.Add(provider);

            QueueNetworkReconnect();
        }

        public void RemovePowerProvider(ApcPowerProviderComponent provider)
        {
            Providers.Remove(provider);

            QueueNetworkReconnect();
        }

        public void AddConsumer(PowerConsumerComponent consumer)
        {
            consumer.NetworkLoad.LinkedNetwork = default;
            Consumers.Add(consumer);
            QueueNetworkReconnect();
        }

        public void RemoveConsumer(PowerConsumerComponent consumer)
        {
            consumer.NetworkLoad.LinkedNetwork = default;
            Consumers.Remove(consumer);
            QueueNetworkReconnect();
        }

        public void QueueNetworkReconnect()
        {
            _powerNetSystem?.QueueReconnectApcNet(this);
        }

        protected override void SetNetConnectorNet(IBaseNetConnectorComponent<IApcNet> netConnectorComponent)
        {
            netConnectorComponent.Net = this;
        }

        public override string? GetDebugData()
        {
            if (_powerNetSystem == null)
                return null;

            // This is just recycling the multi-tool examine.

            var ps = _powerNetSystem.GetNetworkStatistics(NetworkNode);

            float storageRatio = ps.InStorageCurrent / Math.Max(ps.InStorageMax, 1.0f);
            float outStorageRatio = ps.OutStorageCurrent / Math.Max(ps.OutStorageMax, 1.0f);
            return @$"Current Supply: {ps.SupplyCurrent:G3}
From Batteries: {ps.SupplyBatteries:G3}
Theoretical Supply: {ps.SupplyTheoretical:G3}
Ideal Consumption: {ps.Consumption:G3}
Input Storage: {ps.InStorageCurrent:G3} / {ps.InStorageMax:G3} ({storageRatio:P1})
Output Storage: {ps.OutStorageCurrent:G3} / {ps.OutStorageMax:G3} ({outStorageRatio:P1})";
        }
    }
}
