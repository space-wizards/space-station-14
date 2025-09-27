using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;

namespace Content.Server.ExCable;

/// <summary>
/// Dummy Node group class for handling the explosive cables.
/// </summary>
[NodeGroup(NodeGroupID.ExCable)]
public sealed class ExCableNodeGroup : BaseNodeGroup
{
}
