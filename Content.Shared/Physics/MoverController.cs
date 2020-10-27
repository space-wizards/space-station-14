#nullable enable
using System;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class MoverController : VirtualController
    {
        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        private const float MaxChange = 0.01f;

        // Essentially we need to store our force somewhere so if the player doesn't do any inputs we aren't naturally resisting any new forces.
        private Vector2 _accumulatedForce = Vector2.Zero;

        private float _forceCap = 10.0f;

        // Essentially do unique shit to make movement feel nicer.
        public void Push(Vector2 velocityDirection, float speed, float frameTime)
        {
            if (ControlledComponent == null)
                return;

            var bodyVelocity = ControlledComponent.LinearVelocity;
            var deltaV = velocityDirection - bodyVelocity;

            if (deltaV.Length <= 0.0)
                return;

            var accel = deltaV; // frameTime;

            // Just because I cbf tabbing anymore; body.LinearVelocity += body.Force * body.InvMass * deltaTime;
            // Thus mass gets cancelled out and so velocity += force * deltaTime
            var force = accel.Normalized * Math.Min(accel.Length, MaxChange) * ControlledComponent.Mass;
            Force = force;
        }

        public void StopMoving(float frameTime)
        {
            if (ControlledComponent == null)
                return;

            var bodyVelocity = ControlledComponent.LinearVelocity;
            var deltaV = -bodyVelocity;

            if (deltaV.Length <= 0.0)
                return;

            var accel = deltaV; // frameTime;

            // Just because I cbf tabbing anymore; body.LinearVelocity += body.Force * body.InvMass * deltaTime;
            // Thus mass gets cancelled out and so velocity += force * deltaTime
            var force = accel.Normalized * Math.Min(accel.Length, MaxChange) * ControlledComponent.Mass;
            Force = force;
        }
    }
}
