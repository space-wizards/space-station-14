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

        /*
         * When we stop pressing inputs we need to stop faster. We can't just use friction because that also
         * affects turnspeed and having friction too high feels like shit for turning.
         * Thus we'll apply an impulse for a few ticks to help us stop faster (but don't continuously apply it so
         * other controllers have a chance to do their thing).
         */
        private byte _counterCounter = 0;

        public void Move(Vector2 velocityDirection, float speed)
        {
            _counterCounter = 0;
            if (ControlledComponent?.Owner.IsWeightless() ?? false)
            {
                return;
            }

            Push(velocityDirection, speed);
        }

        public void Push(Vector2 velocityDirection, float speed)
        {
            //Logger.Debug($"Push is {velocityDirection}");
            var existingVelocity = ControlledComponent?.LinearVelocity ?? Vector2.Zero;

            /*
             * So velocityDirection is the ideal of what our velocity "should" be. We also have a maximum vector
             * we can apply to our existing velocity to get to that direction as well
             */

            var difference = velocityDirection - existingVelocity;

            // Close enough
            if (difference.EqualsApprox(Vector2.Zero, 0.001)) return;

            var velocity = difference * speed;

            // TODO here and below: It's possible to overshoot the difference.

            Impulse = velocity;
        }

        public void StopMoving()
        {
            if (ControlledComponent == null)
                return;

            var difference = -ControlledComponent.LinearVelocity;
            if (difference.EqualsApprox(Vector2.Zero, 0.1) || _counterCounter > 15)
            {
                _counterCounter = 16;
                return;
            }

            _counterCounter++;

            var velocity = difference * 20f / 2;
            Impulse = velocity;
        }
    }
}
