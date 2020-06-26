using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    public abstract class DisposalTubeComponent : Component
    {
        /// <summary>
        /// The DisposalNet that this tube is connected to
        /// </summary>
        [ViewVariables]
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
        private Container Contents { get; set; }

        /// <summary>
        /// Collection of entities that are currently inside this tube
        /// </summary>
        public IReadOnlyCollection<IEntity> ContainedEntities => Contents.ContainedEntities;

        protected bool TryInsert(DisposableComponent disposable)
        {
            if (Parent == null)
            {
                return false;
            }

            Contents.Insert(disposable.Owner);
            Parent.Insert(disposable);
            return true;
        }

        protected bool TryRemove(DisposableComponent disposable)
        {
            Contents.Remove(disposable.Owner);
            Parent?.Remove(disposable);
            return true;
        }

        public void SpreadDisposalNet()
        {
            var entityManager = IoCManager.Resolve<IServerEntityManager>();
            var snapGrid = Owner.GetComponent<SnapGridComponent>();
            var tubes = snapGrid.GetCardinalNeighborCells()
                .SelectMany(x => x.GetLocal())
                .Distinct()
                .Select(x => x.TryGetComponent<DisposalTubeComponent>(out var c) ? c : null)
                .Where(x => x != null)
                .Distinct()
                .ToArray();

            if (Parent == null || Reconnecting)
            {
                foreach (var tube in tubes)
                {
                    if (tube.CanConnectTo(out var parent))
                    {
                        ConnectToNet(parent);
                        break;
                    }
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
                    Parent.MergeNets(tube.Parent);
                }
            }
        }

        private bool CanConnectTo([NotNullWhen(true)] out DisposalNet parent)
        {
            parent = Parent;
            return parent != null && !Parent.Dirty && !Reconnecting;
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

        public override void Initialize()
        {
            base.Initialize();
            Contents = ContainerManagerComponent.Ensure<Container>(nameof(Parent), Owner);
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
