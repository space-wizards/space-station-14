using System.Diagnostics.CodeAnalysis;
using Content.Shared.NodeContainer.Components;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.Systems;

public sealed partial class PowerNetConnectorSystem : EntitySystem
{
    [Dependency] private PowerNetHandler _handler = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerNetworkConnectorComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<PowerNetworkConnectorComponent, ComponentShutdown>(OnRemove);
    }

    private void OnRemove(Entity<PowerNetworkConnectorComponent> ent, ref ComponentShutdown args)
    {
        ClearNet(ent);
    }

    private void OnInit(Entity<PowerNetworkConnectorComponent> ent, ref MapInitEvent args)
    {
        TryFindAndSetNet(ent);
    }

    public void TryFindAndSetNet(Entity<PowerNetworkConnectorComponent> ent)
    {
        if (ent.Comp.Voltage != null)
        {
            if (TryFindNet(ent, out var net))
                ent.Comp.Net = net;
        }
        else
        {
            FindAndSetNets(ent);
        }
    }

    public void ClearNet(Entity<PowerNetworkConnectorComponent> ent)
    {
        if (ent.Comp.Net == null)
            return;

        _handler.RemoveConnector(ent.Comp.Net.Value, ent);
        ent.Comp.Net = null;
    }

    private bool TryFindNet(Entity<PowerNetworkConnectorComponent> ent, [NotNullWhen(true)] out Entity<PowerNetComponent>? foundNet)
    {
        if (TryComp(ent, out NodeContainerComponent? container))
        {
            foreach (var node in container.Nodes.Values)
            {
                if (ent.Comp.NodeId == null || ent.Comp.NodeId == node.Name)
                    continue;

                if (!TryComp(node.NodeGroup, out PowerNetComponent? net)
                    || net.Voltage != ent.Comp.Voltage)
                    continue;

                foundNet = (node.NodeGroup.Value.Owner, net);
                return true;
            }
        }
        foundNet = null;
        return false;
    }

    private void FindAndSetNets(Entity<PowerNetworkConnectorComponent> ent)
    {
        if (!TryComp(ent, out NodeContainerComponent? container)
            || ent.Comp.Voltages == null)
            return;

        foreach (var net in container.Nodes.Values)
        {
            if (!ent.Comp.Voltages.TryGetValue(net.Name, out var voltage)
                || !TryComp(net.NodeGroup, out PowerNetComponent? netGroup)
                || netGroup.Voltage != voltage)
                continue;

            ent.Comp.Nets ??= new();
            ent.Comp.Nets.Add(net.Name, (net.NodeGroup.Value.Owner, netGroup));
        }
    }

    public void SetNet(Entity<PowerNetworkConnectorComponent> ent, Entity<PowerNetComponent>? newNet)
    {
        if (ent.Comp.Net != null)
            _handler.RemoveConnector(ent.Comp.Net.Value, ent);

        if (newNet != null)
            _handler.AddConnector(newNet.Value, ent);

        ent.Comp.Net = newNet;
    }

    public void SetVoltage(Entity<PowerNetworkConnectorComponent> ent, Voltage newVoltage)
    {
        ClearNet(ent);
        ent.Comp.Voltage = newVoltage;
        TryFindAndSetNet(ent);
    }
}
