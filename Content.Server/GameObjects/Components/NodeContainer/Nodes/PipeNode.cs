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
    //TODO: Make ConnectedDirections set properly

    /// <summary>
    ///     Connects with other <see cref="PipeNode"/>s whose <see cref="PipeNode.PipeDirection"/>
    ///     correctly correspond.
    /// </summary>
    public class PipeNode : Node, IGasMixtureHolder, IRotatableNode
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public PipeDirection SetPipeDirectionAndSprite { get => PipeDirection; set => AdjustPipeDirectionAndSprite(value); }

        [ViewVariables]
        public PipeDirection PipeDirection { get; private set; }

        /// <summary>
        ///     The directions in which this node is connected to other nodes.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private PipeDirection ConnectedDirections { get => _connectedDirections; set { _connectedDirections = value; UpdateAppearance(); } }
        private PipeDirection _connectedDirections;

        [ViewVariables]
        private IPipeNet _pipeNet = PipeNet.NullNet;

        [ViewVariables]
        private bool _needsPipeNet = true;

        private bool AdjustingSprite { get; set; }

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
            serializer.DataField(this, x => x.PipeDirection, "pipeDirection", PipeDirection.None);
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
            if (AdjustingSprite)
                return;

            var diff = ev.NewRotation - ev.OldRotation;
            PipeDirection = PipeDirection.RotatePipeDirection(diff);
            RefreshNodeGroup();
            UpdateAppearance();
        }

        protected override IEnumerable<Node> GetReachableNodes()
        {
            for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
            {
                var pipeDirection = (PipeDirection) (1 << i);

                var ownNeededConnection = pipeDirection;
                var theirNeededConnection = ownNeededConnection.GetOpposite();
                if (!PipeDirection.HasDirection(ownNeededConnection))
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
                            if (node is PipeNode pipeNode && pipeNode.PipeDirection.HasDirection(theirNeededConnection))
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
            _appearance?.SetData(PipeVisuals.VisualState, new PipeVisualState(PipeDirection.PipeDirectionToPipeShape(), ConnectedDirections));
        }

        /// <summary>
        ///     Changes the directions of this pipe while ensuring the sprite is correctly rotated.
        /// </summary>
        public void AdjustPipeDirectionAndSprite(PipeDirection newDir)
        {
            AdjustingSprite = true;

            var baseDir = newDir.PipeDirectionToPipeShape().ToBaseDirection();

            var newAngle = Angle.FromDegrees(0);

            for (double angle = 0; angle <= 270; angle += 90)  //iterate through angles of cardinal 
            {
                if (baseDir.RotatePipeDirection(Angle.FromDegrees(angle)) == newDir) //finds what angle the entity needs to be rotated from the base to be set to the correct direction
                {
                    newAngle = Angle.FromDegrees(angle);
                    break;
                }
            }

            Owner.Transform.LocalRotation = Angle.FromDegrees(0);
            Owner.Transform.LocalRotation = newAngle;
            PipeDirection = newDir;
            RefreshNodeGroup();
            UpdateAppearance();
            AdjustingSprite = false;
        }
    }
}
