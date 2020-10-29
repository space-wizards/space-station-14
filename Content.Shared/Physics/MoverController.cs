#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class MoverController : VirtualController
    {
        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        private float thrust = 275f; //thrust in newtons
        private float mass = 85f; //don't do this kids
        private float friction_coeff = 1f; //friction coefficient
        private float time_to_vmax = 5f;

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

        public void ApplyForce(Vector2 force, float frameTime)
        {
            if (ControlledComponent == null) return;
            float friction_coeff = 5 / time_to_vmax;
            force *= friction_coeff;
            Vector2 friction;
            if(ControlledComponent.LinearVelocity.Length > 0)
            {
                friction = -ControlledComponent.LinearVelocity * friction_coeff;
            }
            else
            {
                friction = Vector2.Zero;
            }
            force += friction;
            Force = force * frameTime;

        }

        public void StopMoving()
        {
            //LinearVelocity = Vector2.Zero;
        }
    }
}
