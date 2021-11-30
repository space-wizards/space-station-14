using System.Collections.Generic;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Electrocution
{
    [DataDefinition]
    public sealed class ElectrocutionNode : Node
    {
        [DataField("cable")]
        public EntityUid CableEntity;
        [DataField("node")]
        public string NodeName = default!;

        public override IEnumerable<Node> GetReachableNodes()
        {
            var ent = IoCManager.Resolve<IEntityManager>();
            if (!ent.TryGetComponent(CableEntity, out NodeContainerComponent? nodeContainer))
                yield break;

            if (nodeContainer.TryGetNode(NodeName, out Node? node))
                yield return node;
        }
    }
}
