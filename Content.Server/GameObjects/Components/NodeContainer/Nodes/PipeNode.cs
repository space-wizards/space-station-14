using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     Connects with other <see cref="PipeNode"/>s whose <see cref="PipeNode.PipeDirection"/>
    ///     correctly correspond.
    /// </summary>
    public class PipeNode : Node, IGasMixtureHolder, IRotatableNode
    {
        [ViewVariables]
        public PipeDirection PipeDirection { get => _pipeDirection; set => SetPipeDirection(value); }
        private PipeDirection _pipeDirection;

        [ViewVariables]
        private IPipeNet _pipeNet = PipeNet.NullNet;

        [ViewVariables]
        private bool _needsPipeNet = true;

        /// <summary>
        ///     The gases in this pipe.
        /// </summary>
        [ViewVariables]
        public GasMixture Air
        {
            get => _needsPipeNet ? LocalAir : _pipeNet.Air;
            set
            {
                if (_needsPipeNet)
                    LocalAir = value;
                else
                    _pipeNet.Air = value;
            }
        }

        /// <summary>
        ///     Stores gas in this pipe when disconnected from a <see cref="IPipeNet"/>.
        ///     Only for usage by <see cref="IPipeNet"/>s.
        /// </summary>
        [ViewVariables]
        public GasMixture LocalAir { get; set; }

        [ViewVariables]
        public float Volume => LocalAir.Volume;

        private AppearanceComponent _appearance;

        private const float DefaultVolume = 1;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _pipeDirection, "pipeDirection", PipeDirection.None);
            serializer.DataField(this, x => x.LocalAir, "gasMixture", new GasMixture(DefaultVolume));
        }

        public override void Initialize(IEntity owner)
        {
            base.Initialize(owner);
            Owner.TryGetComponent(out _appearance);
            UpdateAppearance();
        }

        public void JoinPipeNet(IPipeNet pipeNet)
        {
            _pipeNet = pipeNet;
            _needsPipeNet = false;
        }

        public void ClearPipeNet()
        {
            _pipeNet = PipeNet.NullNet;
            _needsPipeNet = true;
        }

        void IRotatableNode.RotateEvent(RotateEvent ev)
        {
            var diff = ev.NewRotation - ev.OldRotation;
            PipeDirection = PipeDirection.RotatePipeDirection(diff);
        }

        protected override IEnumerable<Node> GetReachableNodes()
        {
            for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
            {
                var pipeDirection = (PipeDirection) (1 << i);

                var ownNeededConnection = pipeDirection;
                var theirNeededConnection = ownNeededConnection.GetOpposite();
                if (!_pipeDirection.HasDirection(ownNeededConnection))
                {
                    continue;
                }

                var pipeNodesInDirection = new List<PipeNode>();

                var entities = Owner.GetComponent<SnapGridComponent>()
                    .GetInDir(pipeDirection.ToDirection());

                foreach (var entity in entities)
                {
                    if (entity.TryGetComponent<NodeContainerComponent>(out var container))
                    {
                        foreach (var node in container.Nodes)
                        {
                            if (node is PipeNode pipeNode && pipeNode._pipeDirection.HasDirection(theirNeededConnection))
                            {
                                pipeNodesInDirection.Add(pipeNode);
                            }
                        }
                    }
                }

                foreach (var pipeNode in pipeNodesInDirection)
                {
                    yield return pipeNode;
                }
            }
        }

        private void UpdateAppearance()
        {
            _appearance?.SetData(PipeVisuals.VisualState, new PipeVisualState(PipeDirection));
        }

        private void SetPipeDirection(PipeDirection pipeDirection)
        {
            _pipeDirection = pipeDirection;
            RefreshNodeGroup();
            UpdateAppearance();
        }
    }
}
