using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.Power;

namespace Content.Server.Power.EntitySystems;

public sealed partial class PowerNetConnectorSystem : EntitySystem
{
    [Dependency] private PowerNetHandler _handler = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerNetworkConnectorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PowerNetworkConnectorComponent, ComponentRemove>(OnRemove);
    }

    private void OnRemove(Entity<PowerNetworkConnectorComponent> ent, ref ComponentRemove args)
    {
        ClearNet(ent);
    }

    private void OnInit(Entity<PowerNetworkConnectorComponent> ent, ref ComponentInit args)
    {
        TryFindAndSetNet(ent);
    }

    public void TryFindAndSetNet(Entity<PowerNetworkConnectorComponent> ent)
    {
        if (TryFindNet(ent, out var net))
        {
            ent.Comp.Net = net;
        }
    }

    public void ClearNet(Entity<PowerNetworkConnectorComponent> ent)
    {
        if (ent.Comp.Net == null)
            return;

        _handler.RemoveConnector(ent.Comp.Net, ent);
        ent.Comp.Net = null;
    }

    private bool TryFindNet(Entity<PowerNetworkConnectorComponent> ent, [NotNullWhen(true)] out PowerNet? foundNet)
    {
        if (TryComp(ent, out NodeContainerComponent? container))
        {
            var compatibleNet = container.Nodes.Values
                .Where(node => (ent.Comp.NodeId == null || ent.Comp.NodeId == node.Name) && node.NodeGroupID == (NodeGroupID) ent.Comp.Voltage)
                .Select(node => node.NodeGroup)
                .OfType<PowerNet>()
                .FirstOrDefault();

            if (compatibleNet != null)
            {
                foundNet = compatibleNet;
                return true;
            }
        }
        foundNet = null;
        return false;
    }

    public void SetNet(Entity<PowerNetworkConnectorComponent> ent, PowerNet? newNet)
    {
        if (ent.Comp.Net != null)
            _handler.RemoveConnector(ent.Comp.Net, ent);

        if (newNet != null)
            _handler.AddConnector(newNet, ent);

        ent.Comp.Net = newNet;
    }

    public void SetVoltage(Entity<PowerNetworkConnectorComponent> ent, Voltage newVoltage)
    {
        ClearNet(ent);
        ent.Comp.Voltage = newVoltage;
        TryFindAndSetNet(ent);
    }
}
