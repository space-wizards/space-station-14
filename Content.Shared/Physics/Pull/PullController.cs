#nullable enable
using System;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Content.Shared.GameObjects.Components.Pulling;
using static Content.Shared.GameObjects.EntitySystems.SharedInteractionSystem;

namespace Content.Shared.Physics.Pull
{
    /// <summary>
    /// This is applied upon a Pullable object when that object is being pulled.
    /// It lives only to serve that Pullable object.
    /// </summary>
    public class PullController : VirtualController
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        private const float DistBeforeStopPull = InteractionRange;

        private const float StopMoveThreshold = 0.25f;

        /// <summary>
        /// The managing SharedPullableComponent of this PullController.
        /// MUST BE SET! If you go attaching PullControllers yourself, YOU ARE DOING IT WRONG.
        /// If you get a crash based on such, then, well, see previous note.
        /// This is set by the SharedPullableComponent attaching the PullController.
        /// </summary>
        public SharedPullableComponent Manager = default!;

        private EntityCoordinates? _movingTo;

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
            var _puller = Manager.PullerPhysics;
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

        public bool TryMoveTo(EntityCoordinates from, EntityCoordinates to)
        {
            var _puller = Manager.PullerPhysics;
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
            var _puller = Manager.PullerPhysics;
            if (_puller == null || ControlledComponent == null)
            {
                return;
            }

            if (!_puller.Owner.IsInSameOrNoContainer(ControlledComponent.Owner))
            {
                Manager.Puller = null;
                return;
            }

            var distance = _puller.Owner.Transform.WorldPosition - ControlledComponent.Owner.Transform.WorldPosition;

            if (distance.Length > DistBeforeStopPull)
            {
                Manager.Puller = null;
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

            if (LinearVelocity != Vector2.Zero)
            {
                var angle = LinearVelocity.ToAngle();
                ControlledComponent.Owner.Transform.LocalRotation = angle;
            }
        }
    }
}
