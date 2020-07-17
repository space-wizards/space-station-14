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

        private PhysicsComponent _controlledComponent;

        private PhysicsComponent _puller;

        public bool GettingPulled => _puller != null;

        public override PhysicsComponent ControlledComponent
        {
            set => _controlledComponent = value;
        }

        public void StartPull(PhysicsComponent pull)
        {
            _puller = pull;
        }

        public void StopPull()
        {
            _puller = null;
            _controlledComponent.RemoveController();
        }

        public void MoveTo(GridCoordinates puller, GridCoordinates to)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!puller.InRange(mapManager, to, SharedInteractionSystem.InteractionRange))
            {
                return;
            }

            var dir = to.Position - puller.Position;
            var ray = new CollisionRay(puller.Position, dir.Normalized, (int) CollisionGroup.Impassable);
            var physicsManager = IoCManager.Resolve<IPhysicsManager>();
            var rayResults = physicsManager.IntersectRayWithPredicate(puller.ToMap(mapManager).MapId, ray, dir.Length).ToList();
            var position = rayResults.Count > 0
                ? new GridCoordinates(rayResults[0].HitPos, to.GridID)
                : to;

            var dist = _puller.Owner.Transform.GridPosition.Position - position.Position;

            if (Math.Sqrt(dist.LengthSquared) > DistBeforeStopPull ||
                Math.Sqrt(dist.LengthSquared) < 0.25f)
            {
                return;
            }

            _controlledComponent.Owner.Transform.GridPosition = position;
        }

        public override void UpdateBeforeProcessing()
        {
            base.UpdateBeforeProcessing();

            if (_puller == null) return;

            // Are we outside of pulling range?
            var dist = _puller.Owner.Transform.WorldPosition - _controlledComponent.Owner.Transform.WorldPosition;

            if (dist.Length > DistBeforeStopPull)
            {
                _puller.Owner.GetComponent<ISharedHandsComponent>().StopPulling();
            }
            else if (dist.Length > DistBeforePull)
            {
                _controlledComponent.LinearVelocity = dist.Normalized * _puller.LinearVelocity.Length * 1.1f;
            }
            else
            {
                _controlledComponent.LinearVelocity = Vector2.Zero;
            }
        }
    }
}
