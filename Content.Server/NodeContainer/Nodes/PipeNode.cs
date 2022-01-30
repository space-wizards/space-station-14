using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.NodeContainer.Nodes
{
    /// <summary>
    ///     Connects with other <see cref="PipeNode"/>s whose <see cref="PipeDirection"/>
    ///     correctly correspond.
    /// </summary>
    [DataDefinition]
    public class PipeNode : Node, IGasMixtureHolder, IRotatableNode
    {
        private PipeDirection _connectedDirections;

        /// <summary>
        ///     The directions in which this pipe can connect to other pipes around it.
        /// </summary>
        [ViewVariables]
        [DataField("pipeDirection")]
        private PipeDirection _originalPipeDirection;

        /// <summary>
        ///     The *current* pipe directions (accounting for rotation)
        ///     Used to check if this pipe can connect to another pipe in a given direction.
        /// </summary>
        public PipeDirection CurrentPipeDirection { get; private set; }

        private HashSet<PipeNode>? _alwaysReachable;

        public void AddAlwaysReachable(PipeNode pipeNode)
        {
            if (NodeGroup == null) return;
            if (pipeNode.NodeGroupID != NodeGroupID) return;
            _alwaysReachable ??= new();
            _alwaysReachable.Add(pipeNode);
            EntitySystem.Get<NodeGroupSystem>().QueueRemakeGroup((BaseNodeGroup) NodeGroup);
        }

        public void RemoveAlwaysReachable(PipeNode pipeNode)
        {
            if (_alwaysReachable == null) return;
            if (NodeGroup == null) return;
            if (pipeNode.NodeGroupID != NodeGroupID) return;
            _alwaysReachable.Remove(pipeNode);
            EntitySystem.Get<NodeGroupSystem>().QueueRemakeGroup((BaseNodeGroup) NodeGroup);
        }

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

                if (NodeGroup != null)
                    EntitySystem.Get<NodeGroupSystem>().QueueRemakeGroup((BaseNodeGroup) NodeGroup);
            }
        }

        [DataField("connectionsEnabled")]
        private bool _connectionsEnabled = true;

        [DataField("rotationsEnabled")]
        public bool RotationsEnabled { get; set; } = true;

        /// <summary>
        ///     The <see cref="IPipeNet"/> this pipe is a part of.
        /// </summary>
        [ViewVariables]
        private IPipeNet? PipeNet => (IPipeNet?) NodeGroup;

        /// <summary>
        ///     The gases in this pipe.
        /// </summary>
        [ViewVariables]
        public GasMixture Air
        {
            get => PipeNet?.Air ?? GasMixture.SpaceGas;
            set
            {
                DebugTools.Assert(PipeNet != null);
                PipeNet!.Air = value;
            }
        }

        public void AssumeAir(GasMixture giver)
        {
            if(PipeNet != null)
                EntitySystem.Get<AtmosphereSystem>().Merge(PipeNet.Air, giver);
        }

        [ViewVariables]
        [DataField("volume")]
        public float Volume { get; set; } = DefaultVolume;

        private const float DefaultVolume = 200f;

        public override void OnContainerStartup()
        {
            base.OnContainerStartup();
            OnConnectedDirectionsNeedsUpdating();
        }

        public override void OnContainerShutdown()
        {
            base.OnContainerShutdown();
            UpdateAdjacentConnectedDirections();
        }

        public void JoinPipeNet(IPipeNet pipeNet)
        {
            OnConnectedDirectionsNeedsUpdating();
        }

        /// <summary>
        ///     Rotates the <see cref="PipeDirection"/> when the entity is rotated, and re-calculates the <see cref="IPipeNet"/>.
        /// </summary>
        void IRotatableNode.RotateEvent(ref RotateEvent ev)
        {
            OnConnectedDirectionsNeedsUpdating();
            UpdateAppearance();
        }

        public override IEnumerable<Node> GetReachableNodes()
        {
            for (var i = 0; i < PipeDirectionHelpers.AllPipeDirections; i++)
            {
                var pipeDir = (PipeDirection) (1 << i);

                if (!CurrentPipeDirection.HasDirection(pipeDir))
                    continue;

                foreach (var pipe in LinkableNodesInDirection(pipeDir))
                {
                    yield return pipe;
                }
            }

            if(_alwaysReachable != null)
            {
                var remQ = new RemQueue<PipeNode>();
                foreach(var pipe in _alwaysReachable)
                {
                    if (pipe.Deleting)
                    {
                        remQ.Add(pipe);
                    }
                    yield return pipe;
                }

                foreach(var pipe in remQ)
                {
                    _alwaysReachable.Remove(pipe);
                }
            }
        }

        /// <summary>
        ///     Gets the pipes that can connect to us from entities on the tile or adjacent in a direction.
        /// </summary>
        private IEnumerable<PipeNode> LinkableNodesInDirection(PipeDirection pipeDir)
        {
            if (!Anchored)
                yield break;

            foreach (var pipe in PipesInDirection(pipeDir))
            {
                if (pipe.ConnectionsEnabled && pipe.CurrentPipeDirection.HasDirection(pipeDir.GetOpposite()))
                    yield return pipe;
            }
        }

        /// <summary>
        ///     Gets the pipes from entities on the tile adjacent in a direction.
        /// </summary>
        protected IEnumerable<PipeNode> PipesInDirection(PipeDirection pipeDir)
        {
            if (!IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Anchored)
                yield break;

            var grid = IoCManager.Resolve<IMapManager>().GetGrid(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).GridID);
            var position = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Coordinates;
            foreach (var entity in grid.GetInDir(position, pipeDir.ToDirection()))
            {
                if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<NodeContainerComponent>(entity, out var container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    if (node is PipeNode pipe)
                        yield return pipe;
                }
            }
        }

        /// <summary>
        ///     Gets the pipes from entities on the same tile.
        /// </summary>
        protected IEnumerable<PipeNode> PipesInTile()
        {
            if (!IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Anchored)
                yield break;

            var grid = IoCManager.Resolve<IMapManager>().GetGrid(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).GridID);
            var position = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Coordinates;
            foreach (var entity in grid.GetLocal(position))
            {
                if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<NodeContainerComponent>(entity, out var container))
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
        ///     Also updates CurrentPipeDirection.
        /// </summary>
        private void OnConnectedDirectionsNeedsUpdating()
        {
            if (RotationsEnabled)
            {
                CurrentPipeDirection = _originalPipeDirection.RotatePipeDirection(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).LocalRotation);
            }
            else
            {
                CurrentPipeDirection = _originalPipeDirection;
            }
            UpdateConnectedDirections();
            UpdateAdjacentConnectedDirections();
            UpdateAppearance();
        }

        /// <summary>
        ///     Checks what directions there are connectable pipes in, to update <see cref="ConnectedDirections"/>.
        /// </summary>
        private void UpdateConnectedDirections()
        {
            ConnectedDirections = PipeDirection.None;

            for (var i = 0; i < PipeDirectionHelpers.AllPipeDirections; i++)
            {
                var pipeDir = (PipeDirection) (1 << i);

                if (!CurrentPipeDirection.HasDirection(pipeDir))
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
                    pipe.UpdateAppearance();
                }
            }
        }

        /// <summary>
        ///     Updates the <see cref="AppearanceComponent"/>.
        ///     Gets the combined <see cref="ConnectedDirections"/> of every pipe on this entity, so the visualizer on this entity can draw the pipe connections.
        /// </summary>
        private void UpdateAppearance()
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out AppearanceComponent? appearance)
                || !IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out NodeContainerComponent? container))
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
