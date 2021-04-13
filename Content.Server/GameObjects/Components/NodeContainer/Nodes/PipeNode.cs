#nullable enable
using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
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
        [DataField("connectionsEnabled")]
        private PipeDirection _connectedDirections;

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
        public PipeDirection ConnectedDirections
        {
            get => _connectedDirections;
            private set
            {
                _connectedDirections = value;
                UpdateAppearance();
            }
        }

        /// <summary>
        ///     Whether this node can connect to others or not.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ConnectionsEnabled
        {
            get => _connectionsEnabled;
            set
            {
                _connectionsEnabled = value;

                if (!_connectionsEnabled)
                {
                    _pipeNet.RemoveNode(this);
                }
            }
        }

        /// <summary>
        ///     The <see cref="IPipeNet"/> this pipe is a part of. Set to <see cref="PipeNet.NullNet"/> when not in an <see cref="IPipeNet"/>.
        /// </summary>
        [ViewVariables]
        private IPipeNet _pipeNet = PipeNet.NullNet;

        private bool _connectionsEnabled = true;

        /// <summary>
        ///     The gases in this pipe.
        /// </summary>
        [ViewVariables]
        public GasMixture Air
        {
            get => _pipeNet.Air;
            set => _pipeNet.Air = value;
        }

        [ViewVariables]
        [DataField("volume")]
        public float Volume { get; set; } = DefaultVolume;

        private const float DefaultVolume = 200f;

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
        }

        public void ClearPipeNet()
        {
            _pipeNet = PipeNet.NullNet;
        }

        /// <summary>
        ///     Rotates the <see cref="PipeDirection"/> when the entity is rotated, and re-calculates the <see cref="IPipeNet"/>.
        /// </summary>
        void IRotatableNode.RotateEvent(RotateEvent ev)
        {
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
                if (pipe.ConnectionsEnabled && pipe.PipeDirection.HasDirection(pipeDir.GetOpposite()))
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
                {
                    pipe.UpdateConnectedDirections();
                }
            }
        }

        /// <summary>
        ///     Updates the <see cref="AppearanceComponent"/>.
        ///     Gets the combined <see cref="ConnectedDirections"/> of every pipe on this entity, so the visualizer on this entity can draw the pipe connections.
        /// </summary>
        private void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearance)
                || !Owner.TryGetComponent(out NodeContainerComponent? container))
                return;

            var netConnectedDirections = PipeDirection.None;

            foreach (var node in container.Nodes.Values)
            {
                if (node is PipeNode pipe)
                {
                    netConnectedDirections |= pipe.ConnectedDirections;
                }
            }

            appearance.SetData(PipeVisuals.VisualState, new PipeVisualState(netConnectedDirections));
        }
    }
}
