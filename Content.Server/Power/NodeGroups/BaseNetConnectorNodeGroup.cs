using System.Collections.Generic;
using System.Linq;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Power.NodeGroups
{
    public abstract class BaseNetConnectorNodeGroup<TNetType> : BaseNodeGroup
    {
        public override void LoadNodes(List<Node> groupNodes)
        {
            base.LoadNodes(groupNodes);

            foreach (var node in groupNodes)
            {
                var newNetConnectorComponents = IoCManager.Resolve<IEntityManager>().GetComponents<IBaseNetConnectorComponent<TNetType>>(node.Owner)
                    .Where(powerComp => (powerComp.NodeId == null || powerComp.NodeId == node.Name) &&
                                        (NodeGroupID) powerComp.Voltage == node.NodeGroupID)
                    .ToList();

                foreach (var netConnector in newNetConnectorComponents)
                {
                    SetNetConnectorNet(netConnector);
                }
            }
        }

        protected abstract void SetNetConnectorNet(IBaseNetConnectorComponent<TNetType> netConnectorComponent);
    }
}
