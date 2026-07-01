using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Shared.Power.Nodes.Handlers;

public sealed class CableDeviceNodeHandler : BaseCableDeviceNodeHandler<CableDeviceNode>
{
    protected override string? GetExamineText(CableDeviceNode node)
    {
        return node.NodeGroupID switch
        {
            NodeGroupID.HVPower => Loc.GetString("node-container-component-on-examine-details-hvpower"),
            NodeGroupID.MVPower => Loc.GetString("node-container-component-on-examine-details-mvpower"),
            NodeGroupID.Apc => Loc.GetString("node-container-component-on-examine-details-apc"),
            _ => null
        };
    }
}
