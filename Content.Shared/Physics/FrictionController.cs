using System;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public abstract class FrictionController : VirtualController
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        public override void UpdateAfterProcessing()
        {
            base.UpdateAfterProcessing();

            if (ControlledComponent != null && !_physicsManager.IsWeightless(ControlledComponent.Owner.Transform.Coordinates))
            {
                LinearVelocity *= 0.85f;
                if (MathF.Abs(LinearVelocity.Length) < 1f)
                    Stop();
            }
        }
    }
}
