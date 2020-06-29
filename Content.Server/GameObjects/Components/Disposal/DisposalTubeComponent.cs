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

        public abstract Direction NextDirection(InDisposalsComponent inDisposals);

        public IDisposalTubeComponent NextTube(InDisposalsComponent inDisposals)
        {
            var nextDirection = NextDirection(inDisposals);
            return Connected.GetValueOrDefault(nextDirection);
        }

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

                var oppositeDirection = new Angle(direction.ToAngle().Theta + Math.PI).GetDir();
                if (!tube.AdjacentConnected(oppositeDirection, this))
                {
                    continue;
                }

                Connected.Add(direction, tube);
            }
        }

        public bool AdjacentConnected(Direction direction, IDisposalTubeComponent tube)
        {
            if (Connected.ContainsKey(direction) ||
                !ConnectableDirections().Contains(direction))
            {
                return false;
            }

            Connected.Add(direction, tube);
            return true;
        }

        private void Disconnect()
        {
            foreach (var connected in Connected.Values)
            {
                connected.AdjacentDisconnected(this);
            }

            Connected.Clear();
        }

        public void AdjacentDisconnected(IDisposalTubeComponent adjacent)
        {
            var outdated = Connected.Where(pair => pair.Value == adjacent).ToArray();

            foreach (var pair in outdated)
            {
                Connected.Remove(pair.Key);
            }
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
