using System.Linq;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Components;
using Content.Shared.NodeContainer.Systems;
using Content.Shared.Power.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Power.Systems;

public sealed partial class PowerNetHandler : NodeGroupHandler<PowerNetComponent>
{
    [Dependency] private SharedPowerNetSystem _powerNetSystem = default!;
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
        NodeGroupSys.NodeGroupTypes.Add(NodeGroupID.HVPower, NodeGroupCompType);
        NodeGroupSys.NodeGroupTypes.Add(NodeGroupID.MVPower, NodeGroupCompType);
        NodeGroupSys.NodeGroupTypes.Add(NodeGroupID.Apc, NodeGroupCompType);
        NodeGroupSys.NodeGroupHandlers.Add(NodeGroupCompType, this);
    }

    protected override void InitializeGroup(Entity<NodeGroupComponent, PowerNetComponent> group, Node sourceNode)
    {
        base.InitializeGroup(group, sourceNode);
        _powerNetSystem.InitPowerNet((group.Owner, group.Comp2));
    }

    protected override void LoadNodes(Entity<NodeGroupComponent, PowerNetComponent> group, List<Node> groupNodes)
    {
        base.LoadNodes(group, groupNodes);
        foreach (var node in groupNodes)
        {
            if (!_connectorQuery.TryComp(node.Owner, out var comp))
                continue;

            if (comp.Voltage != null
                && (comp.NodeId == null || comp.NodeId == node.Name)
                && (NodeGroupID) comp.Voltage == node.NodeGroupID)
            {
                comp.Net = (group.Owner, group.Comp2);
            }
            else if (comp.Voltages != null)
            {
                foreach (var (nodeId, voltage) in comp.Voltages)
                {
                    if (nodeId == node.Name && (NodeGroupID) voltage == node.NodeGroupID)
                    {
                        comp.Net = (group.Owner, group.Comp2);
                    }
                }
            }
        }
    }

    protected override void AfterRemake(Entity<NodeGroupComponent, PowerNetComponent> group, IEnumerable<IGrouping<Entity<NodeGroupComponent>?, Node>> newGroups)
    {
        base.AfterRemake(group, newGroups);
        _powerNetSystem.DestroyPowerNet((group.Owner, group.Comp2));
    }

    public void AddDischarger(Entity<PowerNetComponent> group, Entity<BatteryDischargerComponent?, PowerNetworkBatteryComponent?> discharger)
    {
        if (!_dischargerQuery.Resolve(discharger.Owner, ref discharger.Comp1, false)
            || !_batteryQuery.Resolve(discharger.Owner, ref discharger.Comp2, false))
            return;

        DebugTools.Assert(discharger.Comp2.LinkedNetworkDischarging == default);
        discharger.Comp2.LinkedNetworkDischarging = default;
        group.Comp.Dischargers.Add(discharger!);
        QueueNetworkReconnect(group);
    }

    public void RemoveDischarger(Entity<PowerNetComponent> group, Entity<BatteryDischargerComponent?, PowerNetworkBatteryComponent?> discharger)
    {
        // Can be missing if the entity is being deleted, not a big deal.
        if (TryComp(discharger.Owner, out PowerNetworkBatteryComponent? battery))
        {
            // Linked network can be default if it was re-connected twice in one tick.
            DebugTools.Assert(battery.LinkedNetworkDischarging == default ||
                              battery.LinkedNetworkDischarging == group.Id);
            battery.LinkedNetworkDischarging = default;
        }

        group.Comp.Dischargers.Remove(discharger);
        QueueNetworkReconnect(group);
    }

    public void AddCharger(Entity<PowerNetComponent> group, Entity<BatteryChargerComponent?, PowerNetworkBatteryComponent?> charger)
    {
        if (!_chargerQuery.Resolve(charger.Owner, ref charger.Comp1, false)
            || !_batteryQuery.Resolve(charger.Owner, ref charger.Comp2, false))
            return;

        DebugTools.Assert(charger.Comp2.LinkedNetworkCharging == default);
        charger.Comp2.LinkedNetworkCharging = default;
        group.Comp.Chargers.Add(charger);
        QueueNetworkReconnect(group);
    }

    public void RemoveCharger(Entity<PowerNetComponent> group, Entity<BatteryChargerComponent?, PowerNetworkBatteryComponent?> charger)
    {
        // Can be missing if the entity is being deleted, not a big deal.
        if (TryComp(charger.Owner, out PowerNetworkBatteryComponent? battery))
        {
            // Linked network can be default if it was re-connected twice in one tick.
            DebugTools.Assert(battery.LinkedNetworkCharging == default ||
                              battery.LinkedNetworkCharging == group.Id);
            battery.LinkedNetworkCharging = default;
        }

        group.Comp.Chargers.Remove(charger);
        QueueNetworkReconnect(group);
    }

    public void AddConsumer(Entity<PowerNetComponent> group, Entity<PowerConsumerComponent?> consumer)
    {
        if (!_consumerQuery.Resolve(ref consumer, false) || consumer.Comp == null)
            return;

        DebugTools.Assert(consumer.Comp.LinkedNetwork == default);
        consumer.Comp.LinkedNetwork = default;
        group.Comp.Consumers.Add(consumer!);
        QueueNetworkReconnect(group);
    }

    public void RemoveConsumer(Entity<PowerNetComponent> group, Entity<PowerConsumerComponent?> consumer)
    {
        if (!_consumerQuery.Resolve(ref consumer, false) || consumer.Comp == null)
            return;

        // Linked network can be default if it was re-connected twice in one tick.
        DebugTools.Assert(consumer.Comp.LinkedNetwork == default || consumer.Comp.LinkedNetwork == group.Id);
        consumer.Comp.LinkedNetwork = default;
        group.Comp.Consumers.Remove(consumer!);
        QueueNetworkReconnect(group);
    }

    public void AddSupplier(Entity<PowerNetComponent> group, Entity<PowerSupplierComponent?> supplier)
    {
        if (!_supplierQuery.Resolve(ref supplier, false) || supplier.Comp == null)
            return;

        DebugTools.Assert(supplier.Comp.LinkedNetwork == default);
        supplier.Comp.LinkedNetwork = default;
        group.Comp.Suppliers.Add(supplier);
        QueueNetworkReconnect(group);
    }

    public void RemoveSupplier(Entity<PowerNetComponent> group, Entity<PowerSupplierComponent?> supplier)
    {
        if (!_supplierQuery.Resolve(ref supplier, false) || supplier.Comp == null)
            return;

        // Linked network can be default if it was re-connected twice in one tick.
        DebugTools.Assert(supplier.Comp.LinkedNetwork == default || supplier.Comp.LinkedNetwork == group.Id);
        supplier.Comp.LinkedNetwork = default;
        group.Comp.Suppliers.Remove(supplier);
        QueueNetworkReconnect(group);
    }

    public void QueueNetworkReconnect(Entity<PowerNetComponent> group)
    {
        _powerNetSystem.QueueReconnectPowerNet(group);
    }

    public void AddReceiver(Entity<PowerNetComponent> group, Entity<PowerReceiverComponent?> receiver, Entity<PowerProviderComponent?> provider)
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

    public void RemoveReceiver(Entity<PowerNetComponent> group, Entity<PowerReceiverComponent?> receiver, Entity<PowerProviderComponent?> provider)
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

    public void AddConnector(Entity<PowerNetComponent> group, Entity<PowerNetworkConnectorComponent> ent)
    {
        AddCharger(group, ent.Owner);
        AddDischarger(group, ent.Owner);
        AddSupplier(group, ent.Owner);
        AddConsumer(group, ent.Owner);
    }

    public void RemoveConnector(Entity<PowerNetComponent> group, Entity<PowerNetworkConnectorComponent> ent)
    {
        RemoveCharger(group, ent.Owner);
        RemoveDischarger(group, ent.Owner);
        RemoveSupplier(group, ent.Owner);
        RemoveConsumer(group, ent.Owner);
    }
}
