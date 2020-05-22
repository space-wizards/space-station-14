namespace Content.Server.GameObjects.Components.NodeGroup.NodeGroups
{
    //todo: add overrides for power specific node network functionality, pull power components off owner of nodes being added to group and set their powernet field
    [NodeGroup(NodeGroupID.HVPower, NodeGroupID.MVPower, NodeGroupID.LVPower)]
    public class PowerNodeGroup : NodeGroup
    {

    }

    //todo: add interface for power components to use for adding/updating/removing themselves
    public interface IPowerNetwork
    {

    }
}
