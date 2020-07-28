using System;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class SlipController : VirtualController
    {
        private float Decay { get; set; } = 0.2f;

        private float DecayBy(float number, float by)
        {
            if (Math.Abs(number) < 0.001)
            {
                return 0;
            }

            if (number > 0)
            {
                return Math.Max(number - by, 0);
            }

            return Math.Min(number + by, 0);
        }

        public override void UpdateAfterProcessing()
        {
            var x = DecayBy(LinearVelocity.X, Decay);
            var y = DecayBy(LinearVelocity.Y, Decay);

            LinearVelocity = (x, y);
        }
    }
}
