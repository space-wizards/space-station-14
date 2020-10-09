using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        ///     Controls what visuals are applied in <see cref="PipeVisualizer"/>.
        /// </summary>
        public ConduitLayer ConduitLayer => _conduitLayer;
        private ConduitLayer _conduitLayer;

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
        public float Volume { get; private set; }

        private AppearanceComponent _appearance;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _pipeDirection, "pipeDirection", PipeDirection.None);
            serializer.DataField(this, x => Volume, "volume", 10);
            serializer.DataField(ref _conduitLayer, "conduitLayer", ConduitLayer.Two);
        }

        public override void Initialize(IEntity owner)
        {
            base.Initialize(owner);
            LocalAir = new GasMixture(Volume);
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
            var newPipeDir = PipeDirection.None;
            for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
            {
                var pipeDirection = (PipeDirection) (1 << i);
                if (!PipeDirection.HasFlag(pipeDirection)) continue;
                var angle = pipeDirection.ToAngle();
                angle += diff;
                newPipeDir |= angle.GetCardinalDir().ToPipeDirection();
            }
            PipeDirection = newPipeDir;
        }

        protected override IEnumerable<Node> GetReachableNodes()
        {
            for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
            {
                var pipeDirection = (PipeDirection) (1 << i);

                var ownNeededConnection = pipeDirection;
                var theirNeededConnection = ownNeededConnection.GetOpposite();
                if (!_pipeDirection.HasFlag(ownNeededConnection))
                {
                    continue;
                }
                var pipeNodesInDirection = Owner.GetComponent<SnapGridComponent>()
                    .GetInDir(pipeDirection.ToDirection())
                    .Select(entity => entity.TryGetComponent<NodeContainerComponent>(out var container) ? container : null)
                    .Where(container => container != null)
                    .SelectMany(container => container.Nodes)
                    .OfType<PipeNode>()
                    .Where(pipeNode => pipeNode._pipeDirection.HasFlag(theirNeededConnection));
                foreach (var pipeNode in pipeNodesInDirection)
                {
                    yield return pipeNode;
                }
            }
        }

        private void UpdateAppearance()
        {
            _appearance?.SetData(PipeVisuals.VisualState, new PipeVisualState(PipeDirection, ConduitLayer));
        }

        private void SetPipeDirection(PipeDirection pipeDirection)
        {
            _pipeDirection = pipeDirection;
            RefreshNodeGroup();
            UpdateAppearance();
        }
    }
}
