using System.Collections.Generic;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.NodeContainer.Nodes
{
    [DataDefinition]
    public class PortablePipeNode : PipeNode
    {
        public override IEnumerable<Node> GetReachableNodes()
        {
            foreach (var node in PipesInTile())
            {
                if (node is PortPipeNode)
                    yield return node;
            }

            foreach (var node in base.GetReachableNodes())
            {
                yield return node;
            }
        }
    }
}
