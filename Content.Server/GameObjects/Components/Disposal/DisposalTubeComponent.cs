using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using JetBrains.Annotations;
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
        /// Container of entities that are currently inside this tube
        /// </summary>
        [ViewVariables]
        public Container Contents { get; private set; }

        /// <summary>
        /// Collection of entities that are currently inside this tube
        /// </summary>
        [ViewVariables]
        public IEnumerable<IEntity> ContainedEntities => Contents.ContainedEntities;

        /// <summary>
        /// Dictionary of tubes connecting to this one mapped by their direction
        /// </summary>
        [ViewVariables]
        public Dictionary<Direction, IDisposalTubeComponent> Connectors { get; } = new Dictionary<Direction, IDisposalTubeComponent>();

        // TODO: Change this to be an immutable property
        /// <summary>
        /// The directions that this tube can connect to others from
        /// </summary>
        /// <returns>a new array of the directions</returns>
        protected abstract Direction[] ConnectableDirections();

        public abstract IDisposalTubeComponent NextTube(InDisposalsComponent inDisposals);

        private void Remove(IEntity entity)
        {
            Contents.Remove(entity);

            if (!entity.TryGetComponent(out InDisposalsComponent disposable))
            {
                return;
            }

            disposable.ExitDisposals();
        }

        private void TransferTo(InDisposalsComponent inDisposals, IDisposalTubeComponent to)
        {
            var position = inDisposals.Owner.Transform.LocalPosition;
            if (!to.Contents.Insert(inDisposals.Owner))
            {
                return;
            }

            inDisposals.Owner.Transform.LocalPosition = position;

            Contents.Remove(inDisposals.Owner);
            inDisposals.EnterTube(to);
        }

        private void Connect()
        {
            // TODO: Make disposal pipes extend the grid
            Connectors.Clear();
            var snapGrid = Owner.GetComponent<SnapGridComponent>();

            foreach (var direction in ConnectableDirections())
            {
                var tube = snapGrid
                    .GetInDir(direction)
                    .Select(x => x.TryGetComponent(out IDisposalTubeComponent c) ? c : null)
                    .FirstOrDefault(x => x != null);

                if (tube == null)
                {
                    continue;
                }

                Connectors.Add(direction, tube);
            }
        }

        private void Disconnect()
        {
            foreach (var adjacentTube in Connectors.Values)
            foreach (var outdatedPair in adjacentTube.Connectors.Where(pair => pair.Value == this).ToList())
            {
                adjacentTube.Connectors.Remove(outdatedPair.Key);
            }

            Connectors.Clear();
        }

        public void Update(float frameTime, IEntity entity)
        {
            if (!Contents.Contains(entity))
            {
                Logger.Warning($"{nameof(DisposalTubeComponent)} {nameof(Update)} called on a non contained entity {entity.Name} at grid position {entity.Transform.GridPosition.ToString()}");
                return;
            }

            if (!entity.TryGetComponent(out InDisposalsComponent inDisposals))
            {
                Remove(entity);
                return;
            }

            while (frameTime > 0)
            {
                var time = frameTime;
                if (time > inDisposals.TimeLeft)
                {
                    time = inDisposals.TimeLeft;
                }

                inDisposals.TimeLeft -= time;
                frameTime -= time;

                var snapGrid = Owner.GetComponent<SnapGridComponent>();
                var tubeRotation = Owner.Transform.LocalRotation;

                if (inDisposals.TimeLeft > 0)
                {
                    var progress = 1 - inDisposals.TimeLeft / inDisposals.StartingTime;
                    var newPosition = tubeRotation.ToVec() * progress;
                    newPosition = (-newPosition.X, newPosition.Y);

                    entity.Transform.LocalPosition = newPosition;

                    continue;
                }

                var next = snapGrid
                    .GetInDir(tubeRotation.GetDir())
                    .FirstOrDefault(adjacent => adjacent.HasComponent<IDisposalTubeComponent>()); // TODO

                if (next == null)
                {
                    Remove(entity);
                    break;
                }

                var to = next.GetComponent<IDisposalTubeComponent>();
                TransferTo(inDisposals, to);
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
