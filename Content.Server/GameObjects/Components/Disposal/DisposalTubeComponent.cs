using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    public abstract class DisposalTubeComponent : Component, IDisposalTubeComponent, IAnchored, IUnAnchored
    {
        /// <summary>
        ///     Container of entities that are currently inside this tube
        /// </summary>
        [ViewVariables]
        public Container Contents { get; private set; }

        /// <summary>
        ///     Dictionary of tubes connecting to this one mapped by their direction
        /// </summary>
        [ViewVariables]
        public Dictionary<Direction, IDisposalTubeComponent> Connected { get; } =
            new Dictionary<Direction, IDisposalTubeComponent>();

        /// <summary>
        ///     The directions that this tube can connect to others from
        /// </summary>
        /// <returns>a new array of the directions</returns>
        protected abstract Direction[] ConnectableDirections();

        public abstract IDisposalTubeComponent NextTube(InDisposalsComponent inDisposals);

        public bool Remove(InDisposalsComponent inDisposals)
        {
            var removed = Contents.Remove(inDisposals.Owner);
            inDisposals.ExitDisposals();
            return removed;
        }

        public bool TransferTo(InDisposalsComponent inDisposals, IDisposalTubeComponent to)
        {
            var position = inDisposals.Owner.Transform.LocalPosition;
            if (!to.Contents.Insert(inDisposals.Owner))
            {
                return false;
            }

            inDisposals.Owner.Transform.LocalPosition = position;

            Contents.Remove(inDisposals.Owner);
            inDisposals.EnterTube(to);

            return true;
        }

        private void Connect()
        {
            // TODO: Make disposal pipes extend the grid
            Connected.Clear();
            var snapGrid = Owner.GetComponent<SnapGridComponent>();

            foreach (var direction in ConnectableDirections())
            {
                var tube = snapGrid
                    .GetInDir(direction)
                    .Select(x => x.TryGetComponent(out IDisposalTubeComponent c) ? c : null)
                    .FirstOrDefault(x => x != null && x != this);

                if (tube == null)
                {
                    continue;
                }

                Connected.Add(direction, tube);

                var oppositeDirection = new Angle(direction.ToAngle().Theta + Math.PI).GetDir();
                tube.AdjacentConnected(oppositeDirection, this);
            }
        }

        public void AdjacentConnected(Direction direction, IDisposalTubeComponent tube)
        {
            if (Connected.ContainsKey(direction))
            {
                return;
            }

            if (ConnectableDirections().Any(connectable => connectable == direction))
            {
                Connected.Add(direction, tube);
            }
        }

        private void Disconnect()
        {
            foreach (var connectedTube in Connected.Values)
            {
                var outdated = connectedTube.Connected.Where(pair => pair.Value == this).ToArray();

                foreach (var outdatedPair in outdated)
                {
                    connectedTube.Connected.Remove(outdatedPair.Key);
                }
            }

            Connected.Clear();
        }

        public override void Initialize()
        {
            base.Initialize();

            Contents = ContainerManagerComponent.Ensure<Container>(Name, Owner);
            Owner.EnsureComponent<AnchorableComponent>();
        }

        protected override void Startup()
        {
            base.Startup();

            if (Owner.GetComponent<PhysicsComponent>().Anchored)
            {
                Connect();
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Disconnect();
        }

        void IAnchored.Anchored(AnchoredEventArgs eventArgs)
        {
            Connect();
        }

        void IUnAnchored.UnAnchored(UnAnchoredEventArgs eventArgs)
        {
            Disconnect();
        }
    }
}
