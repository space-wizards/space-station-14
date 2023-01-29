using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Pow3r;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using System.Linq;

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
        private PowerNetSystem? _powerNetSystem;
        private IEntityManager? _entMan;

        [ViewVariables] public readonly List<PowerSupplierComponent> Suppliers = new();
        [ViewVariables] public readonly List<PowerConsumerComponent> Consumers = new();
        [ViewVariables] public readonly List<BatteryChargerComponent> Chargers = new();
        [ViewVariables] public readonly List<BatteryDischargerComponent> Dischargers = new();

        [ViewVariables]
        public PowerState.Network NetworkNode { get; } = new();

        public override void Initialize(Node sourceNode, IEntityManager entMan)
        {
            base.Initialize(sourceNode, entMan);

            _entMan = entMan;

            _powerNetSystem = entMan.EntitySysManager.GetEntitySystem<PowerNetSystem>();
            _powerNetSystem.InitPowerNet(this);
        }

        public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
        {
            base.AfterRemake(newGroups);

            _powerNetSystem?.DestroyPowerNet(this);
        }

        protected override void SetNetConnectorNet(IBaseNetConnectorComponent<IPowerNet> netConnectorComponent)
        {
            netConnectorComponent.Net = this;
        }

        public void AddSupplier(PowerSupplierComponent supplier)
        {
            DebugTools.Assert(supplier.NetworkSupply.LinkedNetwork == default);
            supplier.NetworkSupply.LinkedNetwork = default;
            Suppliers.Add(supplier);
            _powerNetSystem?.QueueReconnectPowerNet(this);
        }

        public void RemoveSupplier(PowerSupplierComponent supplier)
        {
            DebugTools.Assert(supplier.NetworkSupply.LinkedNetwork == NetworkNode.Id);
            supplier.NetworkSupply.LinkedNetwork = default;
            Suppliers.Remove(supplier);
            _powerNetSystem?.QueueReconnectPowerNet(this);
        }

        public void AddConsumer(PowerConsumerComponent consumer)
        {
            DebugTools.Assert(consumer.NetworkLoad.LinkedNetwork == default);
            consumer.NetworkLoad.LinkedNetwork = default;
            Consumers.Add(consumer);
            _powerNetSystem?.QueueReconnectPowerNet(this);
        }

        public void RemoveConsumer(PowerConsumerComponent consumer)
        {
            DebugTools.Assert(consumer.NetworkLoad.LinkedNetwork == NetworkNode.Id);
            consumer.NetworkLoad.LinkedNetwork = default;
            Consumers.Remove(consumer);
            _powerNetSystem?.QueueReconnectPowerNet(this);
        }

        public void AddDischarger(BatteryDischargerComponent discharger)
        {
            if (_entMan == null)
                return;

            var battery = _entMan.GetComponent<PowerNetworkBatteryComponent>(discharger.Owner);
            DebugTools.Assert(battery.NetworkBattery.LinkedNetworkDischarging == default);
            battery.NetworkBattery.LinkedNetworkDischarging = default;
            Dischargers.Add(discharger);
            _powerNetSystem?.QueueReconnectPowerNet(this);
        }

        public void RemoveDischarger(BatteryDischargerComponent discharger)
        {
            if (_entMan == null)
                return;

            // Can be missing if the entity is being deleted, not a big deal.
            if (_entMan.TryGetComponent(discharger.Owner, out PowerNetworkBatteryComponent? battery))
            {
                DebugTools.Assert(battery.NetworkBattery.LinkedNetworkDischarging == NetworkNode.Id);
                battery.NetworkBattery.LinkedNetworkDischarging = default;
            }

            Dischargers.Remove(discharger);
            _powerNetSystem?.QueueReconnectPowerNet(this);
        }

        public void AddCharger(BatteryChargerComponent charger)
        {
            if (_entMan == null)
                return;

            var battery = _entMan.GetComponent<PowerNetworkBatteryComponent>(charger.Owner);
            DebugTools.Assert(battery.NetworkBattery.LinkedNetworkCharging == default);
            battery.NetworkBattery.LinkedNetworkCharging = default;
            Chargers.Add(charger);
            _powerNetSystem?.QueueReconnectPowerNet(this);
        }

        public void RemoveCharger(BatteryChargerComponent charger)
        {
            if (_entMan == null)
                return;

            // Can be missing if the entity is being deleted, not a big deal.
            if (_entMan.TryGetComponent(charger.Owner, out PowerNetworkBatteryComponent? battery))
            {
                DebugTools.Assert(battery.NetworkBattery.LinkedNetworkCharging == NetworkNode.Id);
                battery.NetworkBattery.LinkedNetworkCharging = default;
            }

            Chargers.Remove(charger);
            _powerNetSystem?.QueueReconnectPowerNet(this);
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
