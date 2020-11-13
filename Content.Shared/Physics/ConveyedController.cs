#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class ConveyedController : VirtualController
    {
        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        public void Move(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent?.Owner.IsWeightless() ?? false)
            {
                return;
            }

            if (ControlledComponent?.Status == BodyStatus.InAir)
            {
                return;
            }

            LinearVelocity = velocityDirection * speed;
        }

        public override void UpdateAfterProcessing()
        {
            base.UpdateAfterProcessing();

            LinearVelocity = Vector2.Zero;
        }
    }
}
