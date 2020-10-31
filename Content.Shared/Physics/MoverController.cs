#nullable enable
using System;
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
        //private float drag_coeff = 1f; //drag coefficient
        private float time_to_vmax = 0.2f;

        public void Move(Vector2 velocityDirection, float frameTime)
        {
            if (ControlledComponent == null || (ControlledComponent.Owner.IsWeightless()))
            {
                return;
            }
            Vector2 force = velocityDirection;
            //apply a counteracting force to the standard friction between a human and a floor
            float gravity = 9.8f;
            float friction = 0.35f;
            Vector2 antiFriction = force.Normalized * (0.35f * 9.8f / mass / frameTime);


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

        public void ApplyForce(Vector2 force, float frameTime)
        {
            if (ControlledComponent == null) return;
            if (MathHelper.CloseTo(force.Length, 0f)) return;
            Logger.Debug($"Force is currently {force}");
            //apply a counteracting force to the standard friction between a human and a floor
            float gravity = 9.8f;
            float friction = 0.35f;
            Vector2 antiFriction = force.Normalized * (0.35f * 9.8f / mass / frameTime);
            force += antiFriction;

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
