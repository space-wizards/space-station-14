#nullable enable
using System;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Shared.Physics.Pull
{
    public class PullController : VirtualController
    {
        private const float DistBeforePull = 1.0f;

        private const float DistBeforeStopPull = SharedInteractionSystem.InteractionRange;

        private IPhysicsComponent? _puller;

        public bool GettingPulled => _puller != null;

        private EntityCoordinates? _movingTo;

        public IPhysicsComponent? Puller => _puller;

        public void StartPull(IPhysicsComponent puller)
        {
            DebugTools.AssertNotNull(puller);

            if (_puller == puller)
            {
                return;
            }

            _puller = puller;

            if (ControlledComponent == null)
            {
                return;
            }

            ControlledComponent.WakeBody();

            var message = new PullStartedMessage(this, _puller, ControlledComponent);

            _puller.Owner.SendMessage(null, message);
            ControlledComponent.Owner.SendMessage(null, message);
        }

        public void StopPull()
        {
            var oldPuller = _puller;

            if (oldPuller == null)
            {
                return;
            }

            _puller = null;

            if (ControlledComponent == null)
            {
                return;
            }

            ControlledComponent.WakeBody();

            var message = new PullStoppedMessage(this, oldPuller, ControlledComponent);

            oldPuller.Owner.SendMessage(null, message);
            ControlledComponent.Owner.SendMessage(null, message);

            ControlledComponent.TryRemoveController<PullController>();
        }

        public void TryMoveTo(EntityCoordinates from, EntityCoordinates to)
        {
            if (_puller == null || ControlledComponent == null)
            {
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!from.InRange(entityManager, to, SharedInteractionSystem.InteractionRange))
            {
                return;
            }

            ControlledComponent.WakeBody();

            var dist = _puller.Owner.Transform.Coordinates.Position - to.Position;

            if (Math.Sqrt(dist.LengthSquared) > DistBeforeStopPull ||
                Math.Sqrt(dist.LengthSquared) < 0.25f)
            {
                return;
            }

            _movingTo = to;
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

            // Are we outside of pulling range?
            var dist = _puller.Owner.Transform.WorldPosition - ControlledComponent.Owner.Transform.WorldPosition;

            if (dist.Length > DistBeforeStopPull)
            {
                StopPull();
            }
            else if (_movingTo.HasValue)
            {
                var diff = _movingTo.Value.Position - ControlledComponent.Owner.Transform.Coordinates.Position;
                LinearVelocity = diff.Normalized * 5;
            }
            else if (dist.Length > DistBeforePull)
            {
                LinearVelocity = dist.Normalized * _puller.LinearVelocity.Length * 1.1f;
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
                _movingTo = null;
                return;
            }

            if (_movingTo == null)
            {
                return;
            }

            if (ControlledComponent.Owner.Transform.Coordinates.Position.EqualsApprox(_movingTo.Value.Position, 0.01))
            {
                _movingTo = null;
            }
        }
    }
}
