using System.Collections.Generic;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _nodes, "nodes", new List<Node>());
        }

        public override void Initialize()
        {
            base.Initialize();
            foreach (var node in _nodes)
            {
                node.Initialize(Owner);
            }
        }

        protected override void Startup()
        {
            base.Startup();
            foreach (var node in _nodes)
            {
                node.OnContainerStartup();
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
