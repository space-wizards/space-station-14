namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    //todo: add overrides for power specific node network functionality, pull power components off owner of nodes being added to group and set their powernet field
    [NodeGroup(NodeGroupID.HVPower, NodeGroupID.MVPower, NodeGroupID.LVPower)]
    public class PowerNodeGroup : NodeGroup
    {
        private IPowerNetwork _powerNet;
    }

    //todo: add interface for power components to use for adding/updating/removing themselves
    public interface IPowerNetwork
    {

    }

    public class PowerNetwork : IPowerNetwork
    {

    }
}
