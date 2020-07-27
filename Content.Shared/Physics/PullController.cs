#nullable enable
using System;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class PullController : VirtualController
    {
        private const float DistBeforePull = 1.0f;

        private const float DistBeforeStopPull = SharedInteractionSystem.InteractionRange;

        private ICollidableComponent? _puller;

        public bool GettingPulled => _puller != null;

        private GridCoordinates? _movingTo;

        public ICollidableComponent? Puller => _puller;

        public void StartPull(ICollidableComponent? pull)
        {
            _puller = pull;
        }

        public void StopPull()
        {
            _puller = null;
            ControlledComponent?.TryRemoveController<PullController>();
        }

        public void TryMoveTo(GridCoordinates from, GridCoordinates to)
        {
            if (_puller == null || ControlledComponent == null)
            {
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!from.InRange(mapManager, to, SharedInteractionSystem.InteractionRange))
            {
                return;
            }

            var dist = _puller.Owner.Transform.GridPosition.Position - to.Position;

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

            // Are we outside of pulling range?
            var dist = _puller.Owner.Transform.WorldPosition - ControlledComponent.Owner.Transform.WorldPosition;

            if (dist.Length > DistBeforeStopPull)
            {
                _puller.Owner.GetComponent<ISharedHandsComponent>().StopPull();
            }
            else if (_movingTo.HasValue)
            {
                var diff = _movingTo.Value.Position - ControlledComponent.Owner.Transform.GridPosition.Position;
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

            if (ControlledComponent.Owner.Transform.GridPosition.Position.EqualsApprox(_movingTo.Value.Position, 0.01))
            {
                _movingTo = null;
            }
        }
    }
}
