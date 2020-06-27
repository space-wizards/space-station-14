using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    public abstract class DisposalTubeComponent : Component, IDisposalTubeComponent
    {
        /// <summary>
        /// The DisposalNet that this tube is connected to
        /// </summary>
        [ViewVariables, CanBeNull]
        public DisposalNet Parent { get; private set; }

        /// <summary>
        /// Whether or not this tube is in the process of reconnecting to a net
        /// </summary>
        [ViewVariables]
        public bool Reconnecting { get; set; }

        /// <summary>
        /// Container of entities that are currently inside this tube
        /// </summary>
        [ViewVariables]
        public Container Contents { get; set; }

        /// <summary>
        /// Collection of entities that are currently inside this tube
        /// </summary>
        public IEnumerable<IEntity> ContainedEntities => Contents.ContainedEntities;

        private void Remove(IEntity entity)
        {
            Contents.Remove(entity);

            if (!entity.TryGetComponent(out InDisposalsComponent disposable))
            {
                return;
            }

            Parent?.Remove(disposable);
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

        public void SpreadDisposalNet()
        {
            // TODO: Make disposal pipes extend the grid
            var snapGrid = Owner.GetComponent<SnapGridComponent>();
            var tubes = snapGrid.GetCardinalNeighborCells()
                .SelectMany(x => x.GetLocal())
                .Distinct()
                .Select(x => x.TryGetComponent<IDisposalTubeComponent>(out var c) ? c : null)
                .Where(x => x != null)
                .Distinct()
                .ToArray();

            if (Parent == null || Reconnecting)
            {
                foreach (var tube in tubes)
                {
                    if (!tube.CanConnectTo(out var parent))
                    {
                        continue;
                    }

                    ConnectToNet(parent);
                    break;
                }

                if (Parent == null || Reconnecting)
                {
                    ConnectToNet(new DisposalNet());
                }
            }

            foreach (var tube in tubes)
            {
                if (tube.Parent == null || Reconnecting)
                {
                    tube.ConnectToNet(Parent!);
                    tube.SpreadDisposalNet();
                }
                else if (tube.Parent != Parent && !tube.Parent.Dirty)
                {
                    Parent!.MergeNets(tube.Parent);
                }
            }
        }

        public bool CanConnectTo([NotNullWhen(true)] out DisposalNet parent)
        {
            parent = Parent;
            return parent != null && !parent.Dirty && !Reconnecting;
        }

        public void ConnectToNet(DisposalNet net)
        {
            Parent = net;
            Parent.Add(this);
            Reconnecting = false;
        }

        public void DisconnectFromNet()
        {
            Parent?.Remove(this);
            Parent = null;
        }

        public void Update(float frameTime, IEntity entity)
        {
            if (Reconnecting)
            {
                return;
            }

            if (Parent == null || !entity.TryGetComponent(out InDisposalsComponent disposable))
            {
                Remove(entity);
                return;
            }

            while (frameTime > 0)
            {
                var time = frameTime;
                if (time > disposable.TimeLeft)
                {
                    time = disposable.TimeLeft;
                }

                disposable.TimeLeft -= time;
                frameTime -= time;

                var snapGrid = Owner.GetComponent<SnapGridComponent>();
                var tubeRotation = Owner.Transform.LocalRotation;

                if (disposable.TimeLeft > 0)
                {
                    var progress = 1 - disposable.TimeLeft / disposable.StartingTime;
                    var newPosition = tubeRotation.ToVec() * progress;
                    newPosition = (newPosition.Y, newPosition.X);

                    entity.Transform.LocalPosition = newPosition;

                    continue;
                }

                var next = snapGrid
                    .GetInDir(tubeRotation.GetDir())
                    .FirstOrDefault(adjacent =>
                        adjacent.TryGetComponent(out IDisposalTubeComponent tube) &&
                        tube.Parent == Parent); // TODO

                if (next == null)
                {
                    Remove(entity);
                    break;
                }

                var to = next.GetComponent<IDisposalTubeComponent>();
                TransferTo(disposable, to);
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            Contents = ContainerManagerComponent.Ensure<Container>(Name, Owner);
        }

        protected override void Startup()
        {
            base.Startup();

            if (Parent == null)
            {
                SpreadDisposalNet();
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            DisconnectFromNet();
        }
    }
}
