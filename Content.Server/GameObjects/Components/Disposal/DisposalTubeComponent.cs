#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Disposal
{
    public abstract class DisposalTubeComponent : Component, IDisposalTubeComponent, IBreakAct
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private static readonly TimeSpan ClangDelay = TimeSpan.FromSeconds(0.5);
        private TimeSpan _lastClang;

        private bool _connected;
        private bool _broken;
        [DataField("clangSound")]
        private string _clangSound = "/Audio/Effects/clang.ogg";

        /// <summary>
        ///     Container of entities that are currently inside this tube
        /// </summary>
        [ViewVariables]
        public Container Contents { get; private set; } = default!;

        [ViewVariables]
        private bool Anchored =>
            !Owner.TryGetComponent(out PhysicsComponent? physics) ||
            physics.BodyType == BodyType.Static;

        /// <summary>
        ///     The directions that this tube can connect to others from
        /// </summary>
        /// <returns>a new array of the directions</returns>
        protected abstract Direction[] ConnectableDirections();

        public abstract Direction NextDirection(DisposalHolderComponent holder);

        public virtual Vector2 ExitVector(DisposalHolderComponent holder)
        {
            return NextDirection(holder).ToVec();
        }

        protected Direction DirectionTo(IDisposalTubeComponent other)
        {
            return (other.Owner.Transform.WorldPosition - Owner.Transform.WorldPosition).GetDir();
        }

        public IDisposalTubeComponent? NextTube(DisposalHolderComponent holder)
        {
            var nextDirection = NextDirection(holder);
            var oppositeDirection = new Angle(nextDirection.ToAngle().Theta + Math.PI).GetDir();

            var grid = _mapManager.GetGrid(Owner.Transform.GridID);
            var position = Owner.Transform.Coordinates;
            foreach (var entity in grid.GetInDir(position, nextDirection))
            {
                if (!Owner.EntityManager.ComponentManager.TryGetComponent(entity, out IDisposalTubeComponent? tube))
                {
                    continue;
                }

                if (!tube.CanConnect(oppositeDirection, this))
                {
                    continue;
                }

                if (!CanConnect(nextDirection, tube))
                {
                    continue;
                }

                return tube;
            }

            return null;
        }

        public bool Remove(DisposalHolderComponent holder)
        {
            var removed = Contents.Remove(holder.Owner);
            holder.ExitDisposals();
            return removed;
        }

        public bool TransferTo(DisposalHolderComponent holder, IDisposalTubeComponent to)
        {
            var position = holder.Owner.Transform.LocalPosition;
            if (!to.Contents.Insert(holder.Owner))
            {
                return false;
            }

            holder.Owner.Transform.LocalPosition = position;

            Contents.Remove(holder.Owner);
            holder.EnterTube(to);

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
        }

        public bool CanConnect(Direction direction, IDisposalTubeComponent with)
        {
            if (!_connected)
            {
                return false;
            }

            if (_broken)
            {
                return false;
            }

            if (!ConnectableDirections().Contains(direction))
            {
                return false;
            }

            return true;
        }

        private void Disconnect()
        {
            if (!_connected)
            {
                return;
            }

            _connected = false;

            foreach (var entity in Contents.ContainedEntities.ToArray())
            {
                if (!entity.TryGetComponent(out DisposalHolderComponent? holder))
                {
                    continue;
                }

                holder.ExitDisposals();
            }
        }

        public void PopupDirections(IEntity entity)
        {
            var directions = string.Join(", ", ConnectableDirections());

            Owner.PopupMessage(entity, Loc.GetString("{0}", directions));
        }

        private void UpdateVisualState()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearance))
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
            if (!Owner.TryGetComponent(out PhysicsComponent? physics))
            {
                return;
            }

            if (physics.BodyType == BodyType.Static)
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

        public override void Initialize()
        {
            base.Initialize();

            Contents = ContainerHelpers.EnsureContainer<Container>(Owner, Name);
            Owner.EnsureComponent<AnchorableComponent>();
        }

        protected override void Startup()
        {
            base.Startup();

            Owner.EnsureComponent<PhysicsComponent>(out var physicsComponent);
            if (physicsComponent.BodyType != BodyType.Static)
            {
                return;
            }

            Connect();
            UpdateVisualState();
        }

        public override void OnRemove()
        {
            base.OnRemove();

            Disconnect();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
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
                    SoundSystem.Play(Filter.Pvs(Owner), _clangSound, Owner.Transform.Coordinates);
                    break;

                case AnchoredChangedMessage:
                    AnchoredChanged();
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
                data.Text = Loc.GetString("Tube Directions");
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
