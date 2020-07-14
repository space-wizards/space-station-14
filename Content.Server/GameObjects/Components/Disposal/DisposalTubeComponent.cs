using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Disposal;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    // TODO: Make unanchored pipes pullable
    public abstract class DisposalTubeComponent : Component, IDisposalTubeComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private static readonly TimeSpan ClangDelay = TimeSpan.FromSeconds(0.5);
        private TimeSpan _lastClang;

        private string _clangSound;

        /// <summary>
        ///     Container of entities that are currently inside this tube
        /// </summary>
        [ViewVariables]
        public Container Contents { get; private set; }

        /// <summary>
        ///     Dictionary of tubes connecting to this one mapped by their direction
        /// </summary>
        [ViewVariables]
        protected Dictionary<Direction, IDisposalTubeComponent> Connected { get; } =
            new Dictionary<Direction, IDisposalTubeComponent>();

        /// <summary>
        ///     The directions that this tube can connect to others from
        /// </summary>
        /// <returns>a new array of the directions</returns>
        protected abstract Direction[] ConnectableDirections();

        public abstract Direction NextDirection(DisposableComponent disposable);

        public virtual Vector2 ExitVector(DisposableComponent disposable)
        {
            return NextDirection(disposable).ToVec();
        }

        public IDisposalTubeComponent NextTube(DisposableComponent disposable)
        {
            var nextDirection = NextDirection(disposable);
            return Connected.GetValueOrDefault(nextDirection);
        }

        public bool Remove(DisposableComponent disposable)
        {
            var removed = Contents.Remove(disposable.Owner);
            disposable.ExitDisposals();
            return removed;
        }

        public bool TransferTo(DisposableComponent disposable, IDisposalTubeComponent to)
        {
            var position = disposable.Owner.Transform.LocalPosition;
            if (!to.Contents.Insert(disposable.Owner))
            {
                return false;
            }

            disposable.Owner.Transform.LocalPosition = position;

            Contents.Remove(disposable.Owner);
            disposable.EnterTube(to);

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
            foreach (var entity in Contents.ContainedEntities)
            {
                if (!entity.TryGetComponent(out DisposableComponent disposable))
                {
                    continue;
                }

                disposable.ExitDisposals();
            }

            foreach (var connected in Connected.Values)
            {
                connected.AdjacentDisconnected(this);
            }

            Connected.Clear();
        }

        public void AdjacentDisconnected(IDisposalTubeComponent adjacent)
        {
            foreach (var tube in Connected)
            {
                foreach (var entity in Contents.ContainedEntities)
                {
                    if (!entity.TryGetComponent(out DisposableComponent disposable))
                    {
                        continue;
                    }

                    if (disposable.PreviousTube == adjacent)
                    {
                        disposable.PreviousTube = null;
                    }

                    if (disposable.NextTube == adjacent)
                    {
                        disposable.NextTube = null;
                    }
                }

                if (tube.Value == adjacent)
                {
                    Connected.Remove(tube.Key);
                }
            }
        }

        private void AnchoredChanged()
        {
            if (!Owner.TryGetComponent(out PhysicsComponent physics))
            {
                return;
            }

            if (physics.Anchored)
            {
                Anchored();
            }
            else
            {
                UnAnchored();
            }
        }

        private void Anchored()
        {
            Connect();

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DisposalVisuals.Anchored, true);
            }
        }

        private void UnAnchored()
        {
            Disconnect();

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DisposalVisuals.Anchored, false);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _clangSound, "clangSound", "/Audio/effects/clang.ogg");
        }

        public override void Initialize()
        {
            base.Initialize();

            Contents = ContainerManagerComponent.Ensure<Container>(Name, Owner);
            Owner.EnsureComponent<AnchorableComponent>();

            var physics = Owner.EnsureComponent<PhysicsComponent>();

            physics.AnchoredChanged += AnchoredChanged;
        }

        protected override void Startup()
        {
            base.Startup();

            if (!Owner.GetComponent<PhysicsComponent>().Anchored)
            {
                return;
            }

            Connect();

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DisposalVisuals.Anchored, true);
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();

            var physics = Owner.EnsureComponent<PhysicsComponent>();
            physics.AnchoredChanged -= AnchoredChanged;

            Disconnect();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case RelayMovementEntityMessage _:
                    if (_gameTiming.CurTime < _lastClang + ClangDelay)
                    {
                        break;
                    }

                    _lastClang = _gameTiming.CurTime;
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(_clangSound, Owner.Transform.GridPosition);
                    break;
            }
        }
    }
}
