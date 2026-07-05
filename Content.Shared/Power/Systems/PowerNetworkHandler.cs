using System.Linq;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Components;
using Content.Shared.NodeContainer.Systems;
using Content.Shared.Power.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Power.Systems;

public sealed partial class PowerNetworkHandler : NodeGroupHandler<PowerNetComponent>
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

    private static readonly ProtoId<NodeGroupPrototype> HighPowerId = "HVPower";
    private static readonly ProtoId<NodeGroupPrototype> MediumPowerId = "MVPower";
    private static readonly ProtoId<NodeGroupPrototype> ApcPowerId = "Apc";

    public Dictionary<ushort, Voltage> Voltages { get; } = new();

    public override void RegisterGroups()
    {
        var highId = ProtoMan.Index(HighPowerId).GroupId;
        var midId =  ProtoMan.Index(MediumPowerId).GroupId;
        var apcId = ProtoMan.Index(ApcPowerId).GroupId;
        NodeGroupMan.RegisterGroup(highId, NodeGroupCompType);
        NodeGroupMan.RegisterGroup(midId, NodeGroupCompType);
        NodeGroupMan.RegisterGroup(apcId, NodeGroupCompType);

        // TODO POWER un-hardcode this somehow
        // Maybe by turning voltage into a prototype
        // but I don't feel like dealing with this after refactoring damn 30k lines of code
        Voltages.Add(highId, Voltage.High);
        Voltages.Add(midId, Voltage.Medium);
        Voltages.Add(apcId, Voltage.Apc);
    }

    protected override void InitializeGroup(Entity<NodeGroupComponent, PowerNetComponent> group, Node sourceNode)
    {
        base.InitializeGroup(group, sourceNode);
        _powerNetSystem.InitPowerNet((group.Owner, group.Comp2));
        group.Comp2.Voltage = Voltages[sourceNode.NodeGroupID];
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
                && comp.Voltage == Voltages[node.NodeGroupID])
            {
                comp.Net = (group.Owner, group.Comp2);
            }
            else if (comp.Voltages != null)
            {
                foreach (var (nodeId, voltage) in comp.Voltages)
                {
                    if (nodeId == node.Name && voltage == Voltages[node.NodeGroupID])
                    {
                        comp.Net = (group.Owner, group.Comp2);
                    }
                }
            }
        }
    }

    protected override void AfterRemake(Entity<NodeGroupComponent, PowerNetComponent> group, IEnumerable<IGrouping<Entity<NodeGroupComponent>?, Node>> newGroups)
    {
        _powerNetSystem.DestroyPowerNet((group.Owner, group.Comp2));
        base.AfterRemake(group, newGroups);
    }

    public void AddDischarger(Entity<PowerNetComponent> group, Entity<BatteryDischargerComponent?, PowerNetworkBatteryComponent?> discharger, Node node)
    {
        if (!_dischargerQuery.Resolve(discharger.Owner, ref discharger.Comp1, false)
            || !_batteryQuery.Resolve(discharger.Owner, ref discharger.Comp2, false)
            || node.Name != discharger.Comp1.NodeId)
            return;

        DebugTools.Assert(discharger.Comp2.Battery.LinkedNetworkDischarging == default);
        discharger.Comp2.Battery.LinkedNetworkDischarging = default;
        group.Comp.Dischargers.Add(discharger);
        QueueNetworkReconnect(group);
    }

    public void RemoveDischarger(Entity<PowerNetComponent> group, Entity<BatteryDischargerComponent?, PowerNetworkBatteryComponent?> discharger)
    {
        // Can be missing if the entity is being deleted, not a big deal.
        if (Resolve(discharger.Owner, ref discharger.Comp2, false))
        {
            var battery = discharger.Comp2;
            // Linked network can be default if it was re-connected twice in one tick.
            DebugTools.Assert(battery.Battery.LinkedNetworkDischarging == default ||
                              battery.Battery.LinkedNetworkDischarging == group.Comp.Network.Id);
            battery.Battery.LinkedNetworkDischarging = default;
        }

        group.Comp.Dischargers.Remove(discharger);
        QueueNetworkReconnect(group);
    }

    public void AddCharger(Entity<PowerNetComponent> group, Entity<BatteryChargerComponent?, PowerNetworkBatteryComponent?> charger, Node node)
    {
        if (!_chargerQuery.Resolve(charger.Owner, ref charger.Comp1, false)
            || !_batteryQuery.Resolve(charger.Owner, ref charger.Comp2, false)
            || node.Name != charger.Comp1.NodeId)
            return;

        DebugTools.Assert(charger.Comp2.Battery.LinkedNetworkCharging == default);
        charger.Comp2.Battery.LinkedNetworkCharging = default;
        group.Comp.Chargers.Add(charger);
        QueueNetworkReconnect(group);
    }

    public void RemoveCharger(Entity<PowerNetComponent> group, Entity<BatteryChargerComponent?, PowerNetworkBatteryComponent?> charger)
    {
        // Can be missing if the entity is being deleted, not a big deal.
        if (TryComp(charger.Owner, out PowerNetworkBatteryComponent? battery))
        {
            // Linked network can be default if it was re-connected twice in one tick.
            DebugTools.Assert(battery.Battery.LinkedNetworkCharging == default ||
                              battery.Battery.LinkedNetworkCharging == group.Comp.Network.Id);
            battery.Battery.LinkedNetworkCharging = default;
        }

        group.Comp.Chargers.Remove(charger);
        QueueNetworkReconnect(group);
    }

    public void AddConsumer(Entity<PowerNetComponent> group, Entity<PowerConsumerComponent?> consumer, Node node)
    {
        if (!_consumerQuery.Resolve(ref consumer, false)
            || consumer.Comp == null
            || node.Name != consumer.Comp.NodeId)
            return;

        DebugTools.Assert(consumer.Comp.Load.LinkedNetwork == default);
        consumer.Comp.Load.LinkedNetwork = default;
        group.Comp.Consumers.Add(consumer);
        QueueNetworkReconnect(group);
    }

    public void RemoveConsumer(Entity<PowerNetComponent> group, Entity<PowerConsumerComponent?> consumer)
    {
        if (!_consumerQuery.Resolve(ref consumer, false) || consumer.Comp == null)
            return;

        // Linked network can be default if it was re-connected twice in one tick.
        DebugTools.Assert(consumer.Comp.Load.LinkedNetwork == default || consumer.Comp.Load.LinkedNetwork == group.Comp.Network.Id);
        consumer.Comp.Load.LinkedNetwork = default;
        group.Comp.Consumers.Remove(consumer);
        QueueNetworkReconnect(group);
    }

    public void AddSupplier(Entity<PowerNetComponent> group, Entity<PowerSupplierComponent?> supplier, Node node)
    {
        if (!_supplierQuery.Resolve(ref supplier, false)
            || supplier.Comp == null
            || node.Name != supplier.Comp.NodeId)
            return;

        DebugTools.Assert(supplier.Comp.Supply.LinkedNetwork == default);
        supplier.Comp.Supply.LinkedNetwork = default;
        group.Comp.Suppliers.Add(supplier);
        QueueNetworkReconnect(group);
    }

    public void RemoveSupplier(Entity<PowerNetComponent> group, Entity<PowerSupplierComponent?> supplier)
    {
        if (!_supplierQuery.Resolve(ref supplier, false) || supplier.Comp == null)
            return;

        // Linked network can be default if it was re-connected twice in one tick.
        DebugTools.Assert(supplier.Comp.Supply.LinkedNetwork == default || supplier.Comp.Supply.LinkedNetwork == group.Comp.Network.Id);
        supplier.Comp.Supply.LinkedNetwork = default;
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
        receiver.Comp.Load.LinkedNetwork = default;

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
        receiver.Comp.Load.LinkedNetwork = default;

        QueueNetworkReconnect(group);
    }

    public void AddConnector(Entity<PowerNetComponent> group, Entity<PowerNetworkConnectorComponent> ent, Node node)
    {
        AddCharger(group, ent.Owner, node);
        AddDischarger(group, ent.Owner, node);
        AddSupplier(group, ent.Owner, node);
        AddConsumer(group, ent.Owner, node);
    }

    public void RemoveConnector(Entity<PowerNetComponent> group, Entity<PowerNetworkConnectorComponent> ent)
    {
        RemoveCharger(group, ent.Owner);
        RemoveDischarger(group, ent.Owner);
        RemoveSupplier(group, ent.Owner);
        RemoveConsumer(group, ent.Owner);
    }
}
