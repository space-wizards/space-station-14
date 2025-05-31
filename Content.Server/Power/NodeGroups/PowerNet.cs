using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Server.Power.NodeGroups
{
    public interface IPowerNet : IBasePowerNet
    {
        void AddDischarger(BatteryDischargerComponent discharger);

        void RemoveDischarger(BatteryDischargerComponent discharger);

        void AddCharger(BatteryChargerComponent charger);

        void RemoveCharger(BatteryChargerComponent charger);
    }

    [NodeGroup(NodeGroupID.HVPower, NodeGroupID.MVPower)]
    [UsedImplicitly]
    public sealed partial class PowerNet : BasePowerNet<IPowerNet>, IPowerNet
    {
        [ViewVariables] public readonly List<BatteryChargerComponent> Chargers = new();
        [ViewVariables] public readonly List<BatteryDischargerComponent> Dischargers = new();

        public override void Initialize(Node sourceNode, IEntityManager entMan)
        {
            base.Initialize(sourceNode, entMan);
            PowerNetSystem.InitPowerNet(this);
        }

        public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
        {
            base.AfterRemake(newGroups);

            PowerNetSystem?.DestroyPowerNet(this);
        }

        protected override void SetNetConnectorNet(IBaseNetConnectorComponent<IPowerNet> netConnectorComponent)
        {
            netConnectorComponent.Net = this;
        }

        public void AddDischarger(BatteryDischargerComponent discharger)
        {
            if (EntMan == null)
                return;

            var battery = EntMan.GetComponent<PowerNetworkBatteryComponent>(discharger.Owner);
            DebugTools.Assert(battery.NetworkBattery.LinkedNetworkDischarging == default);
            battery.NetworkBattery.LinkedNetworkDischarging = default;
            Dischargers.Add(discharger);
            QueueNetworkReconnect();
        }

        public void RemoveDischarger(BatteryDischargerComponent discharger)
        {
            if (EntMan == null)
                return;

            // Can be missing if the entity is being deleted, not a big deal.
            if (EntMan.TryGetComponent(discharger.Owner, out PowerNetworkBatteryComponent? battery))
            {
                // Linked network can be default if it was re-connected twice in one tick.
                DebugTools.Assert(battery.NetworkBattery.LinkedNetworkDischarging == default || battery.NetworkBattery.LinkedNetworkDischarging == NetworkNode.Id);
                battery.NetworkBattery.LinkedNetworkDischarging = default;
            }

            Dischargers.Remove(discharger);
            QueueNetworkReconnect();
        }

        public void AddCharger(BatteryChargerComponent charger)
        {
            if (EntMan == null)
                return;

            var battery = EntMan.GetComponent<PowerNetworkBatteryComponent>(charger.Owner);
            DebugTools.Assert(battery.NetworkBattery.LinkedNetworkCharging == default);
            battery.NetworkBattery.LinkedNetworkCharging = default;
            Chargers.Add(charger);
            QueueNetworkReconnect();
        }

        public void RemoveCharger(BatteryChargerComponent charger)
        {
            if (EntMan == null)
                return;

            // Can be missing if the entity is being deleted, not a big deal.
            if (EntMan.TryGetComponent(charger.Owner, out PowerNetworkBatteryComponent? battery))
            {
                // Linked network can be default if it was re-connected twice in one tick.
                DebugTools.Assert(battery.NetworkBattery.LinkedNetworkCharging == default || battery.NetworkBattery.LinkedNetworkCharging == NetworkNode.Id);
                battery.NetworkBattery.LinkedNetworkCharging = default;
            }

            Chargers.Remove(charger);
            QueueNetworkReconnect();
        }

        public override void QueueNetworkReconnect()
        {
            PowerNetSystem?.QueueReconnectPowerNet(this);
        }

        public override string? GetDebugData()
        {
            if (PowerNetSystem == null)
                return null;

            // This is just recycling the multi-tool examine.
            var ps = PowerNetSystem.GetNetworkStatistics(NetworkNode);

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
