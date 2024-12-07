using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Server.Power.NodeGroups
{
    public abstract class BaseNetConnectorNodeGroup<TNetType> : BaseNodeGroup
    {
        protected IEntityManager EntMan = default!;

        public override void Initialize(Node sourceNode, IEntityManager entMan)
        {
            base.Initialize(sourceNode, entMan);
            EntMan = entMan;
        }

        public override void LoadNodes(List<Node> groupNodes)
        {
            base.LoadNodes(groupNodes);

            foreach (var node in groupNodes)
            {
                // TODO POWER PERFORMANCE
                // Replace this with TryComps or some other sane way of doing this, the current solution is awful.
                // This allocates an array, copies ALL of an entities components over, and then iterates over them to
                // yield any that implement the interface.
                foreach (var comp in EntMan.GetComponents<IBaseNetConnectorComponent<TNetType>>(node.Owner))
                {
                    if ((comp.NodeId == null ||
                         comp.NodeId == node.Name) &&
                        (NodeGroupID) comp.Voltage == node.NodeGroupID)
                    {
                        SetNetConnectorNet(comp);
                    }
                }
            }
        }

        protected abstract void SetNetConnectorNet(IBaseNetConnectorComponent<TNetType> netConnectorComponent);
    }
}
