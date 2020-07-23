#nullable enable
using System;
using System.Linq;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
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

            var dir = to.Position - from.Position;
            var ray = new CollisionRay(from.Position, dir.Normalized, (int) CollisionGroup.Impassable);
            var physicsManager = IoCManager.Resolve<IPhysicsManager>();
            var rayResults = physicsManager.IntersectRayWithPredicate(from.ToMap(mapManager).MapId, ray, dir.Length).ToList();
            var position = rayResults.Count > 0
                ? new GridCoordinates(rayResults[0].HitPos, to.GridID)
                : to;

            var dist = _puller.Owner.Transform.GridPosition.Position - position.Position;

            if (Math.Sqrt(dist.LengthSquared) > DistBeforeStopPull ||
                Math.Sqrt(dist.LengthSquared) < 0.25f)
            {
                return;
            }

            ControlledComponent.Owner.Transform.GridPosition = position;
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
                _puller.Owner.GetComponent<ISharedHandsComponent>().StopPulling();
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
    }
}
