#nullable enable
using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     Connects with other <see cref="PipeNode"/>s whose <see cref="PipeDirection"/>
    ///     correctly correspond.
    /// </summary>
    [DataDefinition]
    public class PipeNode : Node, IGasMixtureHolder, IRotatableNode
    {
        /// <summary>
        ///     Modifies the <see cref="PipeDirection"/> of this pipe, and ensures the sprite is correctly rotated.
        ///     This is a property for the sake of calling the method via ViewVariables.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public PipeDirection SetPipeDirectionAndSprite { get => PipeDirection; set => AdjustPipeDirectionAndSprite(value); }

        /// <summary>
        ///     The directions in which this pipe can connect to other pipes around it.
        ///     Used to check if this pipe can connect to another pipe in a given direction.
        /// </summary>
        [ViewVariables]
        [DataField("pipeDirection")]
        public PipeDirection PipeDirection { get; private set; }

        /// <summary>
        ///     The directions in which this node is connected to other nodes.
        ///     Used by <see cref="PipeVisualState"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private PipeDirection ConnectedDirections { get => _connectedDirections; set { _connectedDirections = value; UpdateAppearance(); } }
        private PipeDirection _connectedDirections;

        /// <summary>
        ///     The <see cref="IPipeNet"/> this pipe is a part of. Set to <see cref="PipeNet.NullNet"/> when not in an <see cref="IPipeNet"/>.
        /// </summary>
        [ViewVariables]
        private IPipeNet _pipeNet = PipeNet.NullNet;

        /// <summary>
        ///     If <see cref="_pipeNet"/> is set to <see cref="PipeNet.NullNet"/>.
        ///     When true, this pipe may be storing gas in <see cref="LocalAir"/>.
        /// </summary>
        [ViewVariables]
        private bool _needsPipeNet = true;

        /// <summary>
        ///     Prevents rotation events from re-calculating the <see cref="IPipeNet"/>.
        ///     Used while rotating the sprite to the correct orientation while not affecting the pipe.
        /// </summary>
        private bool IgnoreRotation { get; set; }

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
        [DataField("gasMixture")]
        public GasMixture LocalAir { get; set; } = new(DefaultVolume);

        [ViewVariables]
        public float Volume => LocalAir.Volume;

        private AppearanceComponent? _appearance;

        private const float DefaultVolume = 1;

        public override void Initialize(IEntity owner)
        {
            base.Initialize(owner);
            Owner.TryGetComponent(out _appearance);
        }

        public override void OnContainerStartup()
        {
            base.OnContainerStartup();
            OnConnectedDirectionsNeedsUpdating();
            UpdateAppearance();
        }

        public override void OnContainerShutdown()
        {
            base.OnContainerShutdown();
            UpdateAdjacentConnectedDirections();
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

        /// <summary>
        ///     Rotates the <see cref="PipeDirection"/> when the entity is rotated, and re-calculates the <see cref="IPipeNet"/>.
        /// </summary>
        void IRotatableNode.RotateEvent(RotateEvent ev)
        {
            if (IgnoreRotation)
                return;

            var diff = ev.NewRotation - ev.OldRotation;
            PipeDirection = PipeDirection.RotatePipeDirection(diff);
            RefreshNodeGroup();
            OnConnectedDirectionsNeedsUpdating();
            UpdateAppearance();
        }

        protected override IEnumerable<Node> GetReachableNodes()
        {
            for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
            {
                var pipeDir = (PipeDirection) (1 << i);

                if (!PipeDirection.HasDirection(pipeDir))
                    continue;

                foreach (var pipe in LinkableNodesInDirection(pipeDir))
                    yield return pipe;
            }
        }

        /// <summary>
        ///     Gets the pipes that can connect to us from entities on the tile adjacent in a direction.
        /// </summary>
        private IEnumerable<PipeNode> LinkableNodesInDirection(PipeDirection pipeDir)
        {
            foreach (var pipe in PipesInDirection(pipeDir))
            {
                if (pipe.PipeDirection.HasDirection(pipeDir.GetOpposite()))
                    yield return pipe;
            }
        }

        /// <summary>
        ///     Gets the pipes from entities on the tile adjacent in a direction.
        /// </summary>
        private IEnumerable<PipeNode> PipesInDirection(PipeDirection pipeDir)
        {
            if (!Owner.TryGetComponent(out SnapGridComponent? grid))
                yield break;

            var entities = grid.GetInDir(pipeDir.ToDirection());

            foreach (var entity in entities)
            {
                if (!entity.TryGetComponent<NodeContainerComponent>(out var container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    if (node is PipeNode pipe)
                        yield return pipe;
                }
            }
        }

        /// <summary>
        ///     Updates the <see cref="ConnectedDirections"/> of this and all sorrounding pipes.
        /// </summary>
        private void OnConnectedDirectionsNeedsUpdating()
        {
            UpdateConnectedDirections();
            UpdateAdjacentConnectedDirections();
        }

        /// <summary>
        ///     Checks what directions there are connectable pipes in, to update <see cref="ConnectedDirections"/>.
        /// </summary>
        private void UpdateConnectedDirections()
        {
            ConnectedDirections = PipeDirection.None;
            for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
            {
                var pipeDir = (PipeDirection) (1 << i);

                if (!PipeDirection.HasDirection(pipeDir))
                    continue;

                foreach (var pipe in LinkableNodesInDirection(pipeDir))
                {
                    if (pipe.Connectable && pipe.NodeGroupID == NodeGroupID)
                    {
                        ConnectedDirections |= pipeDir;
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Calls <see cref="UpdateConnectedDirections"/> on all adjacent pipes,
        ///     to update their <see cref="ConnectedDirections"/> when this pipe is changed.
        /// </summary>
        private void UpdateAdjacentConnectedDirections()
        {
            for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
            {
                var pipeDir = (PipeDirection) (1 << i);
                foreach (var pipe in LinkableNodesInDirection(pipeDir))
                    pipe.UpdateConnectedDirections();
            }
        }

        /// <summary>
        ///     Updates the <see cref="AppearanceComponent"/>.
        ///     Gets the combined <see cref="ConnectedDirections"/> of every pipe on this entity, so the visualizer on this entity can draw the pipe connections.
        /// </summary>
        private void UpdateAppearance()
        {
            var netConnectedDirections = PipeDirection.None;
            if (Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                foreach (var node in container.Nodes.Values)
                {
                    if (node is PipeNode pipe)
                    {
                        netConnectedDirections |= pipe.ConnectedDirections;
                    }
                }
            }

            _appearance?.SetData(PipeVisuals.VisualState, new PipeVisualState(PipeDirection.PipeDirectionToPipeShape(), netConnectedDirections));
        }

        /// <summary>
        ///     Changes the directions of this pipe while ensuring the sprite is correctly rotated.
        /// </summary>
        public void AdjustPipeDirectionAndSprite(PipeDirection newDir)
        {
            IgnoreRotation = true;

            var baseDir = newDir.PipeDirectionToPipeShape().ToBaseDirection();

            var newAngle = Angle.FromDegrees(0);

            for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
            {
                var pipeDir = (PipeDirection) (1 << i);
                var angle = pipeDir.ToAngle();
                if (baseDir.RotatePipeDirection(angle) == newDir) //finds what angle the entity needs to be rotated from the base to be set to the correct direction
                {
                    newAngle = angle;
                    break;
                }
            }

            Owner.Transform.LocalRotation = newAngle; //rotate the entity so the sprite's new state will be of the correct direction
            PipeDirection = newDir;

            RefreshNodeGroup();
            OnConnectedDirectionsNeedsUpdating();
            UpdateAppearance();
            IgnoreRotation = false;
        }
    }
}
