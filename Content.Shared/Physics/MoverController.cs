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

        private float time_to_vmax = 0.2f;
        private float max_speed = 7f;

        public void Move(Vector2 velocityDirection, float frameTime)
        {
            if (ControlledComponent == null || (ControlledComponent.Owner.IsWeightless()))
            {
                return;
            }
            //apply a counteracting force to the standard friction between a human and a floor
            //TODO: friction should involve mass, but in the current physics system it doesn't so we can't have it here either
            Vector2 antiFriction = velocityDirection.Normalized * (0.35f * 9.8f * frameTime);

            float mass = ControlledComponent.Mass;
            float dragCoeff = 5 / time_to_vmax;
            Vector2 thrustForce = velocityDirection * dragCoeff * mass;
            Vector2 linearVelocity = ControlledComponent.LinearVelocity;

            Vector2 netForce = thrustForce - linearVelocity * dragCoeff * mass;
            Vector2 a = netForce / mass;
            Vector2 k1 = a * frameTime;

            netForce = thrustForce - (linearVelocity + k1/2) * dragCoeff * mass;
            a = netForce / mass;
            Vector2 k2 = a * frameTime;

            netForce = thrustForce - (linearVelocity + k2/2) * dragCoeff * mass;
            a = netForce / mass;
            Vector2 k3 = a * frameTime;

            netForce = thrustForce - (linearVelocity + k3) * dragCoeff * mass;
            a = netForce / mass;
            Vector2 k4 = a * frameTime;

            Vector2 deltaV = (k1 + k2*2 + k3*2 + k4) / 6;
            a = deltaV / frameTime;
            netForce = a * mass;
            Logger.Debug($"LV: {linearVelocity} resulting in net force of {netForce}");
            Force = netForce;

        }

        public void Push(Vector2 velocityDirection, float speed)
        {
            LinearVelocity = velocityDirection * speed;
        }

        public void StopMoving(float frameTime)
        {
            if (ControlledComponent == null) return;
            Vector2 netForce = Vector2.Zero;
            float drag_coeff = 5 / time_to_vmax;
            netForce *= drag_coeff;
            Vector2 dragForce;
            if(ControlledComponent.LinearVelocity.Length > 0)
            {

                dragForce = -ControlledComponent.LinearVelocity * drag_coeff;
            }
            else
            {
                dragForce = Vector2.Zero;
            }
            netForce += dragForce;
            netForce *= ControlledComponent.Mass;

            //Overshoot check
            Vector2 deltaV = netForce * ControlledComponent.InvMass * frameTime;
            if (deltaV.LengthSquared > ControlledComponent.LinearVelocity.LengthSquared)
            {
                Logger.Debug("STOP: Clamping overshoot.");
                Force = -(ControlledComponent.LinearVelocity * ControlledComponent.Mass) / frameTime;
            }
            else
            {
                Force = netForce;
            }
        }

        public void StopMoving()
        {
            Force = Vector2.Zero;
        }
    }
}
