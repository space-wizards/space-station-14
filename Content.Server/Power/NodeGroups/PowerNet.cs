using System.Collections.Generic;
using System.Linq;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Pow3r;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.NodeGroups
{
    public interface IPowerNet : IBasePowerNet
    {
        void AddSupplier(PowerSupplierComponent supplier);

        void RemoveSupplier(PowerSupplierComponent supplier);

        void AddDischarger(BatteryDischargerComponent discharger);

        void RemoveDischarger(BatteryDischargerComponent discharger);

        void AddCharger(BatteryChargerComponent charger);

        void RemoveCharger(BatteryChargerComponent charger);
    }

    [NodeGroup(NodeGroupID.HVPower, NodeGroupID.MVPower)]
    [UsedImplicitly]
    public sealed class PowerNet : BaseNetConnectorNodeGroup<IPowerNet>, IPowerNet
    {
        private readonly PowerNetSystem _powerNetSystem = EntitySystem.Get<PowerNetSystem>();

        [ViewVariables] public readonly List<PowerSupplierComponent> Suppliers = new();
        [ViewVariables] public readonly List<PowerConsumerComponent> Consumers = new();
        [ViewVariables] public readonly List<BatteryChargerComponent> Chargers = new();
        [ViewVariables] public readonly List<BatteryDischargerComponent> Dischargers = new();

        [ViewVariables]
        public PowerState.Network NetworkNode { get; } = new();

        public override void Initialize(Node sourceNode)
        {
            base.Initialize(sourceNode);

            _powerNetSystem.InitPowerNet(this);
        }

        public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
        {
            base.AfterRemake(newGroups);

            _powerNetSystem.DestroyPowerNet(this);
        }

        protected override void SetNetConnectorNet(IBaseNetConnectorComponent<IPowerNet> netConnectorComponent)
        {
            netConnectorComponent.Net = this;
        }

        public void AddSupplier(PowerSupplierComponent supplier)
        {
            supplier.NetworkSupply.LinkedNetwork = default;
            Suppliers.Add(supplier);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void RemoveSupplier(PowerSupplierComponent supplier)
        {
            supplier.NetworkSupply.LinkedNetwork = default;
            Suppliers.Remove(supplier);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void AddConsumer(PowerConsumerComponent consumer)
        {
            consumer.NetworkLoad.LinkedNetwork = default;
            Consumers.Add(consumer);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void RemoveConsumer(PowerConsumerComponent consumer)
        {
            consumer.NetworkLoad.LinkedNetwork = default;
            Consumers.Remove(consumer);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void AddDischarger(BatteryDischargerComponent discharger)
        {
            var battery = IoCManager.Resolve<IEntityManager>().GetComponent<PowerNetworkBatteryComponent>(discharger.Owner);
            battery.NetworkBattery.LinkedNetworkCharging = default;
            Dischargers.Add(discharger);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void RemoveDischarger(BatteryDischargerComponent discharger)
        {
            // Can be missing if the entity is being deleted, not a big deal.
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(discharger.Owner, out PowerNetworkBatteryComponent? battery))
                battery.NetworkBattery.LinkedNetworkCharging = default;

            Dischargers.Remove(discharger);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void AddCharger(BatteryChargerComponent charger)
        {
            var battery = IoCManager.Resolve<IEntityManager>().GetComponent<PowerNetworkBatteryComponent>(charger.Owner);
            battery.NetworkBattery.LinkedNetworkCharging = default;
            Chargers.Add(charger);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void RemoveCharger(BatteryChargerComponent charger)
        {
            // Can be missing if the entity is being deleted, not a big deal.
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(charger.Owner, out PowerNetworkBatteryComponent? battery))
                battery.NetworkBattery.LinkedNetworkCharging = default;

            Chargers.Remove(charger);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public override string? GetDebugData()
        {
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
