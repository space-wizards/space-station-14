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

        private float time_to_vmax = 0.2f;

        public void Move(Vector2 velocityDirection, float frameTime)
        {
            if (ControlledComponent == null || (ControlledComponent.Owner.IsWeightless()))
            {
                return;
            }
            Vector2 force = velocityDirection;
            //apply a counteracting force to the standard friction between a human and a floor
            Vector2 antiFriction = force.Normalized * (0.35f * 9.8f * frameTime);


            float drag_coeff = 5 / time_to_vmax;
            force *= drag_coeff;
            Vector2 dragForce;
            if(ControlledComponent.LinearVelocity.Length > 0)
            {
                dragForce = -ControlledComponent.LinearVelocity * drag_coeff;
            }
            else
            {
                dragForce = Vector2.Zero;
            }
            force += dragForce;
            force += antiFriction;

            Force = force * frameTime;
        }

        public void Push(Vector2 velocityDirection, float speed)
        {
            LinearVelocity = velocityDirection * speed;
        }

        public void StopMoving(float frameTime)
        {
            if (ControlledComponent == null) return;
            Vector2 force = Vector2.Zero;
            float drag_coeff = 5 / time_to_vmax;
            force *= drag_coeff;
            Vector2 dragForce;
            if(ControlledComponent.LinearVelocity.Length > 0)
            {
                dragForce = -ControlledComponent.LinearVelocity * drag_coeff;
            }
            else
            {
                dragForce = Vector2.Zero;
            }
            force += dragForce;

            Force = force * frameTime;
        }

        public void StopMoving()
        {
            Force = Vector2.Zero;
        }
    }
}
