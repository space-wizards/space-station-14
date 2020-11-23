#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class MoverController : VirtualController
    {
        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        public void Move(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent?.Owner.IsWeightless() ?? false)
            {
                return;
            }

            Push(velocityDirection, speed);
        }

        public void Push(Vector2 velocityDirection, float speed)
        {
            LinearVelocity = velocityDirection * speed;
        }

        public void StopMoving()
        {
            LinearVelocity = Vector2.Zero;
        }
    }
}
