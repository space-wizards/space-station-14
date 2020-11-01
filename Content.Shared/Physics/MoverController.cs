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


            float dragCoeff = 5 / time_to_vmax;
            Vector2 thrustForce = velocityDirection * dragCoeff;
            Vector2 dragForce;
            Vector2 linearVelocity = ControlledComponent.LinearVelocity;
            if(ControlledComponent.LinearVelocity.LengthSquared > 0)
            {
                dragForce = -linearVelocity * dragCoeff;
            }
            else
            {
                dragForce = Vector2.Zero;
            }
            Vector2 netForce = thrustForce + antiFriction;
            netForce += dragForce;
            netForce *= ControlledComponent.Mass;
            //Logger.Debug($"MOVE LinearVelocity: {ControlledComponent.LinearVelocity} ThrustForce: {thrustForce} DragForce: {dragForce} NetForce: {netForce}");

            //Overshoot check
            Vector2 newV = (netForce * ControlledComponent.InvMass * frameTime);
            if (newV.LengthSquared > velocityDirection.LengthSquared)
            {
                Logger.Debug("MOVE: Clamping overshoot.");
                Force = ((netForce.Normalized * velocityDirection.Length) * ControlledComponent.Mass) / frameTime;
            }
            else
            {
                Force = netForce;
            }
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
