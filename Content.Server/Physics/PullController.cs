using System;
using Content.Server.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.Physics
{
    public class PullController: VirtualController
    {
        private const float DistBeforePull = 1.0f;

        private const float DistBeforeStopPull = InteractionSystem.InteractionRange;

        private PhysicsComponent _controlledComponent;

        private PhysicsComponent _puller = null;

        public bool GettingPulled => _puller != null;

        public override SharedPhysicsComponent ControlledComponent
        {
            set => _controlledComponent = value as PhysicsComponent;
        }

        public void StartPull(SharedPhysicsComponent pull)
        {
            _puller = (PhysicsComponent) pull;
        }

        public void StopPull()
        {
            _puller = null;
            _controlledComponent.RemoveController();
        }

        public void MoveTo(GridCoordinates coords)
        {
            var position = new GridCoordinates((float) (Math.Floor(coords.X) + 0.5), (float) (Math.Floor(coords.Y) + 0.5), coords.GridID);
            var dist = _puller.Owner.Transform.GridPosition.Position - position.Position;
            if (Math.Sqrt(dist.LengthSquared) > DistBeforeStopPull) return;
            if (Math.Sqrt(dist.LengthSquared) < 0.25f) return;
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
                _puller.Owner.GetComponent<HandsComponent>().StopPulling();
            }
            else if (dist.Length > DistBeforePull)
            {
                _controlledComponent.LinearVelocity = dist.Normalized * _puller.LinearVelocity.Length;
            }
            else
            {
                _controlledComponent.LinearVelocity = Vector2.Zero;
            }
        }
    }
}
