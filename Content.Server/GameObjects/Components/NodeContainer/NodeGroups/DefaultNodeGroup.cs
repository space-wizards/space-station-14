using Content.Server.GameObjects.Components.NodeContainer.Nodes;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    /// <summary>
    ///     A <see cref="NodeGroup"/> that does nothing additional.
    /// </summary>
    [NodeGroup(NodeGroupID.Default)]
    public class DefaultNodeGroup : NodeGroup
    {
        protected override void OnAddNode(INode node) { }
        protected override void OnRemoveNode(INode node) { }
    }
}
