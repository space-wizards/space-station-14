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

        private float _timeToVmax = 0.2f;

        public void Move(Vector2 velocityDirection, float frameTime)
        {
            if (ControlledComponent == null || (ControlledComponent.Owner.IsWeightless()))
            {
                return;
            }
            //apply a counteracting force to the standard friction between a human and a floor
            //TODO: friction should involve mass, but in the current physics system it doesn't so we can't have it here either
            //Vector2 antiFriction = velocityDirection.Normalized * (0.35f * 9.8f * frameTime);

            float mass = ControlledComponent.Mass;
            float dragCoeff = 5 / _timeToVmax;
            Vector2 thrustForce = velocityDirection * dragCoeff * mass;

            Vector2 netForce = thrustForce - LinearVelocity * dragCoeff * mass;
            Vector2 a = netForce / mass;
            Vector2 k1 = a * frameTime;

            netForce = thrustForce - (LinearVelocity + k1/2) * dragCoeff * mass;
            a = netForce / mass;
            Vector2 k2 = a * frameTime;

            netForce = thrustForce - (LinearVelocity + k2/2) * dragCoeff * mass;
            a = netForce / mass;
            Vector2 k3 = a * frameTime;

            netForce = thrustForce - (LinearVelocity + k3) * dragCoeff * mass;
            a = netForce / mass;
            Vector2 k4 = a * frameTime;

            Vector2 deltaV = (k1 + k2*2 + k3*2 + k4) / 6;
            LinearVelocity += deltaV;

            //Overshoot check
            //Vector2 newV = (netForce * ControlledComponent.InvMass * frameTime);
            // Logger.Debug($"DeltaV: {deltaV}");
            // if (deltaV.LengthSquared > velocityDirection.LengthSquared)
            // {
            //     Force = ((netForce.Normalized * velocityDirection.Length) * ControlledComponent.Mass);
            //     Logger.Debug($"MOVE: Clamping overshoot, LV: {linearVelocity} force: {Force}.");
            // }
            // else
            // {
            //     Logger.Debug($"LV: {linearVelocity} resulting in net force of {netForce}");
            //     Force = netForce;
            // }

            Logger.Debug($"MOVE v: {ControlledComponent.TotalLinearVelocity}");

        }

        public void Push(Vector2 velocityDirection, float speed)
        {
            LinearVelocity = velocityDirection * speed;
        }

        public void StopMoving(float frameTime)
        {
            if (ControlledComponent == null || (ControlledComponent.Owner.IsWeightless()))
            {
                return;
            }
            //apply a counteracting force to the standard friction between a human and a floor
            //TODO: friction should involve mass, but in the current physics system it doesn't so we can't have it here either
            //Vector2 antiFriction = velocityDirection.Normalized * (0.35f * 9.8f * frameTime);

            float mass = ControlledComponent.Mass;
            float dragCoeff = 5 / _timeToVmax;

            Vector2 netForce = Vector2.Zero;
            Vector2 a = netForce / mass;
            Vector2 k1 = a * frameTime;

            netForce = - (LinearVelocity + k1/2) * dragCoeff * mass;
            a = netForce / mass;
            Vector2 k2 = a * frameTime;

            netForce = - (LinearVelocity + k2/2) * dragCoeff * mass;
            a = netForce / mass;
            Vector2 k3 = a * frameTime;

            netForce = - (LinearVelocity + k3) * dragCoeff * mass;
            a = netForce / mass;
            Vector2 k4 = a * frameTime;

            Vector2 deltaV = (k1 + k2*2 + k3*2 + k4) / 6;
            LinearVelocity += deltaV;

            //Overshoot check
            //Vector2 newV = (netForce * ControlledComponent.InvMass * frameTime);
            // Logger.Debug($"DeltaV: {deltaV}");
            // if (deltaV.LengthSquared > velocityDirection.LengthSquared)
            // {
            //     Force = ((netForce.Normalized * velocityDirection.Length) * ControlledComponent.Mass);
            //     Logger.Debug($"MOVE: Clamping overshoot, LV: {linearVelocity} force: {Force}.");
            // }
            // else
            // {
            //     Logger.Debug($"LV: {linearVelocity} resulting in net force of {netForce}");
            //     Force = netForce;
            // }

            Logger.Debug($"STOP v: {ControlledComponent.TotalLinearVelocity}");

            //Overshoot check
            // Vector2 deltaV = netForce * ControlledComponent.InvMass * frameTime;
            // if (deltaV.LengthSquared > ControlledComponent.LinearVelocity.LengthSquared)
            // {
            //     Logger.Debug("STOP: Clamping overshoot.");
            //     Force = -(ControlledComponent.LinearVelocity * ControlledComponent.Mass) / frameTime;
            // }
            // else
            // {
            //     Force = netForce;
            // }
        }

        public void StopMoving()
        {
            LinearVelocity = Vector2.Zero;
        }
    }
}
