#nullable enable
using System;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;
using static Content.Shared.GameObjects.EntitySystems.SharedInteractionSystem;

namespace Content.Shared.Physics.Pull
{
    public class PullController : VirtualController
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private const float DistBeforeStopPull = InteractionRange;

        private const float StopMoveThreshold = 0.25f;

        private IPhysicsComponent? _puller;

        public bool GettingPulled => _puller != null;

        private EntityCoordinates? _movingTo;

        public IPhysicsComponent? Puller => _puller;

        public EntityCoordinates? MovingTo
        {
            get => _movingTo;
            set
            {
                if (_movingTo == value || ControlledComponent == null)
                {
                    return;
                }

                _movingTo = value;
                ControlledComponent.WakeBody();
            }
        }

        private float DistanceBeforeStopPull()
        {
            if (_puller == null)
            {
                return 0;
            }

            var aabbSize =  _puller.AABB.Size;

            return (aabbSize.X > aabbSize.Y ? aabbSize.X : aabbSize.Y) + 0.2f;
        }

        public bool StartPull(IEntity entity)
        {
            DebugTools.AssertNotNull(entity);

            if (!entity.TryGetComponent(out IPhysicsComponent? physics))
            {
                return false;
            }

            return StartPull(physics);
        }

        public bool StartPull(IPhysicsComponent puller)
        {
            DebugTools.AssertNotNull(puller);

            if (_puller == puller)
            {
                return false;
            }

            if (ControlledComponent == null)
            {
                return false;
            }

            _puller = puller;

            ControlledComponent.WakeBody();

            var message = new PullStartedMessage(this, _puller, ControlledComponent);

            _puller.Owner.SendMessage(null, message);
            _puller.Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, message);

            ControlledComponent.Owner.SendMessage(null, message);

            return true;
        }

        public bool StopPull()
        {
            var oldPuller = _puller;

            if (oldPuller == null)
            {
                return false;
            }

            _puller = null;

            if (ControlledComponent == null)
            {
                return false;
            }

            ControlledComponent.WakeBody();

            var message = new PullStoppedMessage(this, oldPuller, ControlledComponent);

            oldPuller.Owner.SendMessage(null, message);
            oldPuller.Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, message);

            ControlledComponent.Owner.SendMessage(null, message);

            ControlledComponent.TryRemoveController<PullController>();

            return true;
        }

        public bool TryMoveTo(EntityCoordinates from, EntityCoordinates to)
        {
            if (_puller == null || ControlledComponent == null)
            {
                return false;
            }

            if (!_puller.Owner.Transform.Coordinates.InRange(_entityManager, from, InteractionRange))
            {
                return false;
            }

            if (!_puller.Owner.Transform.Coordinates.InRange(_entityManager, to, InteractionRange))
            {
                return false;
            }

            if (!from.InRange(_entityManager, to, InteractionRange))
            {
                return false;
            }

            if (from.Position.EqualsApprox(to.Position))
            {
                return false;
            }

            if (!_puller.Owner.Transform.Coordinates.TryDistance(_entityManager, to, out var distance) ||
                Math.Sqrt(distance) > DistBeforeStopPull ||
                Math.Sqrt(distance) < StopMoveThreshold)
            {
                return false;
            }

            MovingTo = to;
            return true;
        }

        public override void UpdateBeforeProcessing()
        {
            if (_puller == null || ControlledComponent == null)
            {
                return;
            }

            if (!_puller.Owner.IsInSameOrNoContainer(ControlledComponent.Owner))
            {
                StopPull();
                return;
            }

            var distance = _puller.Owner.Transform.WorldPosition - ControlledComponent.Owner.Transform.WorldPosition;

            if (distance.Length > DistBeforeStopPull)
            {
                StopPull();
            }
            else if (MovingTo.HasValue)
            {
                var diff = MovingTo.Value.Position - ControlledComponent.Owner.Transform.Coordinates.Position;
                LinearVelocity = diff.Normalized * 5;
            }
            else if (distance.Length > DistanceBeforeStopPull())
            {
                LinearVelocity = distance.Normalized * _puller.LinearVelocity.Length * 1.1f;
            }
            else
            {
                LinearVelocity = Vector2.Zero;
            }
        }

        public override void UpdateAfterProcessing()
        {
            base.UpdateAfterProcessing();

            if (ControlledComponent == null)
            {
                MovingTo = null;
                return;
            }

            if (MovingTo != null &&
                ControlledComponent.Owner.Transform.Coordinates.Position.EqualsApprox(MovingTo.Value.Position, 0.01))
            {
                MovingTo = null;
            }
        }
    }
}
