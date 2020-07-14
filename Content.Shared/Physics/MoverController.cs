using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class MoverController : VirtualController
    {
        private Vector2 _velocity;
        private PhysicsComponent _component = null;

        public Vector2 Velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        public override PhysicsComponent ControlledComponent
        {
            set => _component = value;
        }

        public MoverController()
        {
            _velocity = Vector2.Zero;
        }

        public void Move(Vector2 velocityDirection, float speed)
        {
            if (!_component.Owner.HasComponent<MovementIgnoreGravityComponent>() && IoCManager
                .Resolve<IPhysicsManager>().IsWeightless(_component.Owner.Transform.GridPosition)) return;
            Push(velocityDirection, speed);
        }

        public void Push(Vector2 velocityDirection, float speed)
        {
            Velocity = velocityDirection * speed;
        }

        public void StopMoving()
        {
            Velocity = Vector2.Zero;
        }

        public override void UpdateBeforeProcessing()
        {
            base.UpdateBeforeProcessing();

            if (Velocity == Vector2.Zero)
            {
                // Try to stop movement
                _component.LinearVelocity = Vector2.Zero;
            }
            else
            {
                _component.LinearVelocity = Velocity;
            }
        }
    }
}
