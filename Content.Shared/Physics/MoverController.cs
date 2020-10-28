#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class MoverController : VirtualController
    {
        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        private float thrust = 275f; //thrust in newtons
        private float mass = 60; //don't do this kids
        private float friction_coeff = 5f; //friction coefficient

        public void Move(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent?.Owner.IsWeightless() ?? false)
            {
                return;
            }

            Push(velocityDirection, speed);
        }

        public void Push(Vector2 velocityDirection, float speed)
        {
            LinearVelocity = velocityDirection * speed;
        }

        public void ApplyForce(Vector2 force)
        {
            if (ControlledComponent == null) return;
            force *= thrust;
            Vector2 friction = -ControlledComponent.LinearVelocity * friction_coeff;
            force += friction;
            Vector2 acceleration = force / mass;
            ControlledComponent.LinearVelocity  += acceleration;
        }

        public void StopMoving()
        {
            //LinearVelocity = Vector2.Zero;
        }
    }
}
