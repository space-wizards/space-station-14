#nullable enable
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class MoverController : VirtualController
    {
        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        public void Push(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent == null)
                return;

            LinearVelocity = velocityDirection * speed * 2;
        }

        public void StopMoving()
        {
            LinearVelocity = Vector2.Zero;
        }
    }
}
