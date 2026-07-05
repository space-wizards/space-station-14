using Content.Shared.NodeContainer.Components;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.Systems;

public sealed partial class PowerNetworkConnectorSystem : EntitySystem
{
    [Dependency] private PowerNetworkHandler _handler = default!;
    [Dependency] private EntityQuery<NodeContainerComponent> _containerQuery = default!;
    [Dependency] private EntityQuery<PowerNetComponent> _powerNetQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerNetworkConnectorComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<PowerNetworkConnectorComponent, ComponentShutdown>(OnRemove);
    }

    private void OnRemove(Entity<PowerNetworkConnectorComponent> ent, ref ComponentShutdown args)
    {
        Disconnect(ent);
    }

    private void OnInit(Entity<PowerNetworkConnectorComponent> ent, ref MapInitEvent args)
    {
        Connect(ent);
    }

    public void Connect(Entity<PowerNetworkConnectorComponent> ent)
    {
        if (ent.Comp.Voltage != null)
            FindAndSetNet(ent);
        else
            FindAndSetNets(ent);
    }

    public void Disconnect(Entity<PowerNetworkConnectorComponent> ent)
    {
        foreach (var net in ent.Comp.Nets.Values)
        {
            _handler.RemoveConnector(net, ent);
        }

        ent.Comp.Net = null;
        ent.Comp.Nets.Clear();
    }

    private void FindAndSetNet(Entity<PowerNetworkConnectorComponent> ent)
    {
        if (!_containerQuery.TryComp(ent, out var container))
            return;

        foreach (var node in container.Nodes.Values)
        {
            if (ent.Comp.NodeId != null && ent.Comp.NodeId != node.Name)
                continue;

            if (!_powerNetQuery.TryComp(node.NodeGroup, out var net)
                || net.Voltage != ent.Comp.Voltage)
                continue;

            ent.Comp.Net = (node.NodeGroup.Value.Owner, net);
            ent.Comp.Nets.Add(node.Name, (node.NodeGroup.Value.Owner, net));
            _handler.AddConnector((node.NodeGroup.Value.Owner, net), ent, node);
            return;
        }
    }

    private void FindAndSetNets(Entity<PowerNetworkConnectorComponent> ent)
    {
        if (!_containerQuery.TryComp(ent, out var container)
            || ent.Comp.Voltages == null)
            return;

        foreach (var node in container.Nodes.Values)
        {
            if (!ent.Comp.Voltages.TryGetValue(node.Name, out var voltage)
                || !_powerNetQuery.TryComp(node.NodeGroup, out var netGroup)
                || netGroup.Voltage != voltage)
                continue;

            ent.Comp.Nets.Add(node.Name, (node.NodeGroup.Value.Owner, netGroup));
            if (container.Nodes.Count == 1)
                ent.Comp.Net = (node.NodeGroup.Value.Owner, netGroup);

            _handler.AddConnector((node.NodeGroup.Value.Owner, netGroup), ent, node);
        }
    }

    public void SetVoltage(Entity<PowerNetworkConnectorComponent> ent, Voltage newVoltage)
    {
        Disconnect(ent);
        ent.Comp.Voltage = newVoltage;
        Connect(ent);
    }
}
