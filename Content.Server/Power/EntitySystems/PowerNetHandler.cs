using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.NodeContainer.Systems;
using Content.Shared.Power.Components;
using Robust.Shared.Utility;

namespace Content.Server.Power.EntitySystems;

public sealed partial class PowerNetHandler : NodeGroupHandler<PowerNet>
{
    [Dependency] private PowerNetSystem _powerNetSystem = default!;
    // the wall of queries of doom and despair
    [Dependency] private EntityQuery<PowerNetworkConnectorComponent> _connectorQuery = default!;
    [Dependency] private EntityQuery<BatteryDischargerComponent> _dischargerQuery = default!;
    [Dependency] private EntityQuery<BatteryChargerComponent> _chargerQuery = default!;
    [Dependency] private EntityQuery<PowerNetworkBatteryComponent> _batteryQuery = default!;
    [Dependency] private EntityQuery<PowerConsumerComponent> _consumerQuery = default!;
    [Dependency] private EntityQuery<PowerSupplierComponent> _supplierQuery = default!;
    [Dependency] private EntityQuery<PowerReceiverComponent> _receiverQuery = default!;
    [Dependency] private EntityQuery<PowerProviderComponent> _providerQuery = default!;

    public override void RegisterHandler()
    {
        NodeGroupSys.NodeGroupTypes.Add(NodeGroupID.HVPower, typeof(PowerNet));
        NodeGroupSys.NodeGroupTypes.Add(NodeGroupID.MVPower, typeof(PowerNet));
        NodeGroupSys.NodeGroupTypes.Add(NodeGroupID.Apc, typeof(PowerNet));
        NodeGroupSys.NodeGroupHandlers.Add(typeof(PowerNet), this);
    }

    protected override void InitializeGroup(PowerNet group, Node sourceNode)
    {
        base.InitializeGroup(group, sourceNode);
        _powerNetSystem.InitPowerNet(group);
    }

    protected override void LoadNodes(PowerNet group, List<Node> groupNodes)
    {
        base.LoadNodes(group, groupNodes);
        foreach (var node in groupNodes)
        {
            if (!_connectorQuery.TryComp(node.Owner, out var comp))
                continue;

            if ((comp.NodeId == null ||
                 comp.NodeId == node.Name) &&
                (NodeGroupID) comp.Voltage == node.NodeGroupID)
            {
                SetNetConnectorNet(group, (node.Owner, comp));
            }
        }
    }

    protected override void AfterRemake(PowerNet group, IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        base.AfterRemake(group, newGroups);
        _powerNetSystem.DestroyPowerNet(group);
    }

    public void SetNetConnectorNet(PowerNet group, Entity<PowerNetworkConnectorComponent> netConnectorComponent)
    {
        netConnectorComponent.Comp.Net = group;
    }

    public void AddDischarger(PowerNet group, Entity<BatteryDischargerComponent?, PowerNetworkBatteryComponent?> discharger)
    {
        if (!_dischargerQuery.Resolve(discharger.Owner, ref discharger.Comp1, false)
            || !_batteryQuery.Resolve(discharger.Owner, ref discharger.Comp2, false))
            return;

        DebugTools.Assert(discharger.Comp2.LinkedNetworkDischarging == default);
        discharger.Comp2.LinkedNetworkDischarging = default;
        group.Dischargers.Add(discharger!);
        QueueNetworkReconnect(group);
    }

    public void RemoveDischarger(PowerNet group, Entity<BatteryDischargerComponent?, PowerNetworkBatteryComponent?> discharger)
    {
        // Can be missing if the entity is being deleted, not a big deal.
        if (TryComp(discharger.Owner, out PowerNetworkBatteryComponent? battery))
        {
            // Linked network can be default if it was re-connected twice in one tick.
            DebugTools.Assert(battery.LinkedNetworkDischarging == default ||
                              battery.LinkedNetworkDischarging == group.Id);
            battery.LinkedNetworkDischarging = default;
        }

        group.Dischargers.Remove(discharger);
        QueueNetworkReconnect(group);
    }

    public void AddCharger(PowerNet group, Entity<BatteryChargerComponent?, PowerNetworkBatteryComponent?> charger)
    {
        if (!_chargerQuery.Resolve(charger.Owner, ref charger.Comp1, false)
            || !_batteryQuery.Resolve(charger.Owner, ref charger.Comp2, false))
            return;

        DebugTools.Assert(charger.Comp2.LinkedNetworkCharging == default);
        charger.Comp2.LinkedNetworkCharging = default;
        group.Chargers.Add(charger);
        QueueNetworkReconnect(group);
    }

    public void RemoveCharger(PowerNet group, Entity<BatteryChargerComponent?, PowerNetworkBatteryComponent?> charger)
    {
        // Can be missing if the entity is being deleted, not a big deal.
        if (TryComp(charger.Owner, out PowerNetworkBatteryComponent? battery))
        {
            // Linked network can be default if it was re-connected twice in one tick.
            DebugTools.Assert(battery.LinkedNetworkCharging == default ||
                              battery.LinkedNetworkCharging == group.Id);
            battery.LinkedNetworkCharging = default;
        }

        group.Chargers.Remove(charger);
        QueueNetworkReconnect(group);
    }

    public bool IsConnectedNetwork(PowerNet group) => group.NodeCount > 1;

    public void AddConsumer(PowerNet group, Entity<PowerConsumerComponent?> consumer)
    {
        if (!_consumerQuery.Resolve(ref consumer, false) || consumer.Comp == null)
            return;

        DebugTools.Assert(consumer.Comp.LinkedNetwork == default);
        consumer.Comp.LinkedNetwork = default;
        group.Consumers.Add(consumer!);
        QueueNetworkReconnect(group);
    }

    public void RemoveConsumer(PowerNet group, Entity<PowerConsumerComponent?> consumer)
    {
        if (!_consumerQuery.Resolve(ref consumer, false) || consumer.Comp == null)
            return;

        // Linked network can be default if it was re-connected twice in one tick.
        DebugTools.Assert(consumer.Comp.LinkedNetwork == default || consumer.Comp.LinkedNetwork == group.Id);
        consumer.Comp.LinkedNetwork = default;
        group.Consumers.Remove(consumer!);
        QueueNetworkReconnect(group);
    }

    public void AddSupplier(PowerNet group, Entity<PowerSupplierComponent?> supplier)
    {
        if (!_supplierQuery.Resolve(ref supplier, false) || supplier.Comp == null)
            return;

        DebugTools.Assert(supplier.Comp.LinkedNetwork == default);
        supplier.Comp.LinkedNetwork = default;
        group.Suppliers.Add(supplier);
        QueueNetworkReconnect(group);
    }

    public void RemoveSupplier(PowerNet group, Entity<PowerSupplierComponent?> supplier)
    {
        if (!_supplierQuery.Resolve(ref supplier, false) || supplier.Comp == null)
            return;

        // Linked network can be default if it was re-connected twice in one tick.
        DebugTools.Assert(supplier.Comp.LinkedNetwork == default || supplier.Comp.LinkedNetwork == group.Id);
        supplier.Comp.LinkedNetwork = default;
        group.Suppliers.Remove(supplier);
        QueueNetworkReconnect(group);
    }

    public void QueueNetworkReconnect(PowerNet group)
    {
        _powerNetSystem.QueueReconnectPowerNet(group);
    }

    public void AddReceiver(PowerNet group, Entity<PowerReceiverComponent?> receiver, Entity<PowerProviderComponent?> provider)
    {
        if (!_receiverQuery.Resolve(ref receiver, false)
            || receiver.Comp == null
            || !_providerQuery.Resolve(ref provider, false)
            || provider.Comp == null)
            return;

        provider.Comp.LinkedReceivers.Add(receiver);
        receiver.Comp.LinkedNetwork = default;

        QueueNetworkReconnect(group);
    }

    public void RemoveReceiver(PowerNet group, Entity<PowerReceiverComponent?> receiver, Entity<PowerProviderComponent?> provider)
    {
        if (!_receiverQuery.Resolve(ref receiver, false)
            || receiver.Comp == null
            || !_providerQuery.Resolve(ref provider, false)
            || provider.Comp == null)
            return;

        provider.Comp.LinkedReceivers.Remove(receiver);
        receiver.Comp.LinkedNetwork = default;

        QueueNetworkReconnect(group);
    }

    public void AddConnector(PowerNet group, Entity<PowerNetworkConnectorComponent> ent)
    {
        AddCharger(group, ent.Owner);
        AddDischarger(group, ent.Owner);
        AddSupplier(group, ent.Owner);
        AddConsumer(group, ent.Owner);
    }

    public void RemoveConnector(PowerNet group, Entity<PowerNetworkConnectorComponent> ent)
    {
        RemoveCharger(group, ent.Owner);
        RemoveDischarger(group, ent.Owner);
        RemoveSupplier(group, ent.Owner);
        RemoveConsumer(group, ent.Owner);
    }
}
