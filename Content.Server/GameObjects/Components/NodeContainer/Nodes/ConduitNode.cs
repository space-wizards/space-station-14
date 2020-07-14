using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    public abstract class ConduitNode : Node
    {
        [ViewVariables]
        public ConduitLayer ConduitLayer { get => _conduitLayer; set => SetConduitLayer(value); }
        private ConduitLayer _conduitLayer;

        private void SetConduitLayer(ConduitLayer conduitLayer)
        {
            _conduitLayer = conduitLayer; //todo - make work
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _conduitLayer, "conduitLayer", ConduitLayer.First);
        }

        protected override IEnumerable<Node> GetReachableNodes()
        {
            return GetMatchingLayerNodes(GetNodesToConsider())
                .Where(node => node != null && node != this);
        }

        /// <summary>
        ///     Filters out <see cref="ConduitNode"/>s with a different <see cref="ConduitNode.ConduitLayer"/> from a set of <see cref="Node"/>s.
        ///     Does not remove non-<see cref="ConduitNode"/> <see cref="Node"/>s.
        /// </summary>
        protected IEnumerable<Node> GetMatchingLayerNodes(IEnumerable<Node> nodes)
        {
            foreach (var node in nodes)
            {
                if (!(node is ConduitNode conduitNode) || (conduitNode.ConduitLayer & ConduitLayer) != ConduitLayer.None)
                {
                    yield return node;
                }
            }
        }

        /// <summary>
        ///     Gets a set of <see cref="Node"/>s that this should consider connecting to.
        /// </summary>
        protected abstract IEnumerable<Node> GetNodesToConsider();
    }

    public enum ConduitLayer
    {
        None     = 0,
        First    = 1 << 0,
        Second   = 1 << 1,
        Third    = 1 << 2,
    }
}
