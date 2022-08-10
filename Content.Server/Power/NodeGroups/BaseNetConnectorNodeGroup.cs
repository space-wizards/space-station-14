using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;

namespace Content.Server.Power.NodeGroups
{
    public abstract class BaseNetConnectorNodeGroup<TNetType> : BaseNodeGroup
    {
        public override void LoadNodes(List<Node> groupNodes)
        {
            base.LoadNodes(groupNodes);
            var entManager = IoCManager.Resolve<IEntityManager>();

            foreach (var node in groupNodes)
            {
                var newNetConnectorComponents = new List<IBaseNetConnectorComponent<TNetType>>();

                foreach (var comp in entManager.GetComponents<IBaseNetConnectorComponent<TNetType>>(node.Owner))
                {
                    if ((comp.NodeId == null ||
                         comp.NodeId == node.Name) &&
                        (NodeGroupID) comp.Voltage == node.NodeGroupID)
                    {
                        newNetConnectorComponents.Add(comp);
                    }
                }

                foreach (var netConnector in newNetConnectorComponents)
                {
                    SetNetConnectorNet(netConnector);
                }
            }
        }

        protected abstract void SetNetConnectorNet(IBaseNetConnectorComponent<TNetType> netConnectorComponent);
    }
}
