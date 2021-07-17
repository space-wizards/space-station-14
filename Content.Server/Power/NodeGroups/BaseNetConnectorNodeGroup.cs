using System.Collections.Generic;
using System.Linq;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;

namespace Content.Server.Power.NodeGroups
{
    public abstract class BaseNetConnectorNodeGroup<TNetConnector, TNetType> : BaseNodeGroup
        where TNetConnector : BaseNetConnectorComponent<TNetType>
    {
        public override void LoadNodes(List<Node> groupNodes)
        {
            base.LoadNodes(groupNodes);

            foreach (var node in groupNodes)
            {
                var newNetConnectorComponents = node.Owner
                    .GetAllComponents<TNetConnector>()
                    .Where(powerComp => (powerComp.NodeId == null || powerComp.NodeId == node.Name) &&
                                        (NodeGroupID) powerComp.Voltage == node.NodeGroupID)
                    .ToList();

                foreach (var netConnector in newNetConnectorComponents)
                {
                    SetNetConnectorNet(netConnector);
                }
            }
        }

        protected abstract void SetNetConnectorNet(TNetConnector netConnectorComponent);
    }
}
