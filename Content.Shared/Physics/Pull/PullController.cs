#nullable enable
using System;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
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
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        private const float DistBeforeStopPull = InteractionRange;

        private const float StopMoveThreshold = 0.25f;

        private IPhysicsComponent? _puller;

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

        private bool PullerMovingTowardsPulled()
        {
            if (_puller == null)
            {
                return false;
            }

            if (ControlledComponent == null)
            {
                return false;
            }

            if (_puller.LinearVelocity.EqualsApprox(Vector2.Zero))
            {
                return false;
            }

            var pullerTransform = _puller.Owner.Transform;
            var origin = pullerTransform.Coordinates.Position;
            var velocity = _puller.LinearVelocity.Normalized;
            var mapId = pullerTransform.MapPosition.MapId;
            var ray = new CollisionRay(origin, velocity, (int) CollisionGroup.AllMask);
            bool Predicate(IEntity e) => e != ControlledComponent.Owner;
            var rayResults =
                _physicsManager.IntersectRayWithPredicate(mapId, ray, DistBeforeStopPull, Predicate);

            return rayResults.Any();
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

            var pullAttempt = new PullAttemptMessage(puller, ControlledComponent);

            puller.Owner.SendMessage(null, pullAttempt);

            if (pullAttempt.Cancelled)
            {
                return false;
            }

            ControlledComponent.Owner.SendMessage(null, pullAttempt);

            if (pullAttempt.Cancelled)
            {
                return false;
            }

            _puller = puller;

            var message = new PullStartedMessage(this, _puller, ControlledComponent);

            _puller.Owner.SendMessage(null, message);
            ControlledComponent.Owner.SendMessage(null, message);

            _puller.Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, message);

            ControlledComponent.WakeBody();

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

            var message = new PullStoppedMessage(oldPuller, ControlledComponent);

            oldPuller.Owner.SendMessage(null, message);
            ControlledComponent.Owner.SendMessage(null, message);

            oldPuller.Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, message);

            ControlledComponent.WakeBody();
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
            else
            {
                if (PullerMovingTowardsPulled())
                {
                    LinearVelocity = Vector2.Zero;
                    return;
                }

                var distanceAbs = Vector2.Abs(distance);
                var totalAabb = _puller.AABB.Size + ControlledComponent.AABB.Size / 2;
                if (distanceAbs.X < totalAabb.X && distanceAbs.Y < totalAabb.Y)
                {
                    LinearVelocity = Vector2.Zero;
                    return;
                }

                LinearVelocity = distance.Normalized * _puller.LinearVelocity.Length * 1.5f;
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
