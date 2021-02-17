using System.Collections.Generic;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
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
        [YamlField("nodes")]
        private List<Node> _nodes = new();

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
