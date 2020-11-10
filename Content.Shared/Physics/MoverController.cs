#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class MoverController : VirtualController
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        /*
         * When we stop pressing inputs we need to stop faster. We can't just use friction because that also
         * affects turnspeed and having friction too high feels like shit for turning.
         * Thus we'll apply an impulse for a few ticks to help us stop faster (but don't continuously apply it so
         * other controllers have a chance to do their thing).
         */

        // Values below just WIP as I fuck around with fixing other stuff
        private float _maxImpulse = 40.0f;

        public void Push(Vector2 velocityDirection, float speed)
        {
            //Logger.Debug($"Push is {velocityDirection}");
            var existingVelocity = ControlledComponent?.LinearVelocity ?? Vector2.Zero;

            /*
             * So velocityDirection is the ideal of what our velocity "should" be. We also have a maximum vector
             * we can apply to our existing velocity to get to that direction as well
             */

            var difference = velocityDirection * speed * _maxImpulse * 12 - existingVelocity;

            // Close enough
            if (difference.EqualsApprox(Vector2.Zero, 0.001)) return;

            Vector2 velocity;

            if (difference.Length > _maxImpulse * 12)
            {
                velocity = difference.Normalized * _maxImpulse * 12;
            }
            else
            {
                velocity = difference;
            }

            // TODO here and below: It's possible to overshoot the difference.

            Impulse = velocity;
        }

        public void StopMoving()
        {

        }
    }
}
