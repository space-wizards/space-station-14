using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.NodeContainer
{
    /// <summary>
    ///     Creates and maintains a set of <see cref="Node"/>s.
    /// </summary>
    [RegisterComponent]
    public class NodeContainerComponent : Component
    {
        public override string Name => "NodeContainer";

        [ViewVariables]
        public IReadOnlyList<Node> Nodes => _nodes;
        private List<Node> _nodes = new List<Node>();

#pragma warning disable 649
        [Dependency] private readonly INodeFactory _nodeFactory;
#pragma warning restore 649

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }

        protected override void Startup()
        {
            base.Startup();
            foreach (var node in _nodes)
            {
                node.Owner = Owner;
                node.OnContainerInitialize();
            }
        }

        public override void OnRemove()
        {
            foreach (var node in _nodes)
            {
                node.OnContainerRemove();
            }
            base.OnRemove();
        }
    }
}
