using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.Interfaces;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    // TODO: Make unanchored pipes pullable
    public abstract class DisposalTubeComponent : Component, IDisposalTubeComponent, IBreakAct
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private static readonly TimeSpan ClangDelay = TimeSpan.FromSeconds(0.5);
        private TimeSpan _lastClang;

        private bool _connected;
        private bool _broken;
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

        [ViewVariables]
        private bool Anchored =>
            !Owner.TryGetComponent(out CollidableComponent collidable) ||
            collidable.Anchored;

        /// <summary>
        ///     The directions that this tube can connect to others from
        /// </summary>
        /// <returns>a new array of the directions</returns>
        public abstract Direction[] ConnectableDirections();

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

        // TODO: Make disposal pipes extend the grid
        private void Connect()
        {
            if (_connected || _broken)
            {
                return;
            }

            _connected = true;

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
            if (_broken)
            {
                return false;
            }

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
            if (!_connected)
            {
                return;
            }

            _connected = false;

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
            foreach (var pair in Connected)
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

                if (pair.Value == adjacent)
                {
                    Connected.Remove(pair.Key);
                }
            }
        }

        public void MoveEvent(MoveEvent moveEvent)
        {
            if (!_connected)
            {
                return;
            }

            foreach (var tube in Connected.Values)
            {
                var distance = (tube.Owner.Transform.WorldPosition - Owner.Transform.WorldPosition).Length;

                // Disconnect distance threshold
                if (distance < 1.25)
                {
                    continue;
                }

                AdjacentDisconnected(tube);
                tube.AdjacentDisconnected(this);
            }
        }

        public void PopupDirections(IEntity entity)
        {
            var directions = string.Join(", ", ConnectableDirections());

            Owner.PopupMessage(entity, Loc.GetString("{0}", directions));
        }

        private void UpdateVisualState()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                return;
            }

            var state = _broken
                ? DisposalTubeVisualState.Broken
                : Anchored
                    ? DisposalTubeVisualState.Anchored
                    : DisposalTubeVisualState.Free;

            appearance.SetData(DisposalTubeVisuals.VisualState, state);
        }

        private void AnchoredChanged()
        {
            if (!Owner.TryGetComponent(out CollidableComponent collidable))
            {
                return;
            }

            if (collidable.Anchored)
            {
                OnAnchor();
            }
            else
            {
                OnUnAnchor();
            }
        }

        private void OnAnchor()
        {
            Connect();
            UpdateVisualState();
        }

        private void OnUnAnchor()
        {
            Disconnect();
            UpdateVisualState();
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

            var collidable = Owner.EnsureComponent<CollidableComponent>();

            collidable.AnchoredChanged += AnchoredChanged;
        }

        protected override void Startup()
        {
            base.Startup();

            if (!Owner.GetComponent<CollidableComponent>().Anchored)
            {
                return;
            }

            Connect();
            UpdateVisualState();
        }

        public override void OnRemove()
        {
            base.OnRemove();

            var collidable = Owner.EnsureComponent<CollidableComponent>();
            collidable.AnchoredChanged -= AnchoredChanged;

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

        void IBreakAct.OnBreak(BreakageEventArgs eventArgs)
        {
            _broken = true; // TODO: Repair
            Disconnect();
            UpdateVisualState();
        }

        [Verb]
        private sealed class TubeDirectionsVerb : Verb<IDisposalTubeComponent>
        {
            protected override void GetData(IEntity user, IDisposalTubeComponent component, VerbData data)
            {
                data.Text = "Tube Directions";
                data.CategoryData = VerbCategories.Debug;
                data.Visibility = VerbVisibility.Invisible;

                var groupController = IoCManager.Resolve<IConGroupController>();

                if (user.TryGetComponent<IActorComponent>(out var player))
                {
                    if (groupController.CanCommand(player.playerSession, "tubeconnections"))
                    {
                        data.Visibility = VerbVisibility.Visible;
                    }
                }
            }

            protected override void Activate(IEntity user, IDisposalTubeComponent component)
            {
                var groupController = IoCManager.Resolve<IConGroupController>();

                if (user.TryGetComponent<IActorComponent>(out var player))
                {
                    if (groupController.CanCommand(player.playerSession, "tubeconnections"))
                    {
                        component.PopupDirections(user);
                    }
                }
            }
        }
    }
}
