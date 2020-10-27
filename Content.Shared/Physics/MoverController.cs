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
            // TODO: This is being double-checked 4 no raisin
            if (ControlledComponent?.Owner.IsWeightless() ?? false)
            {
                return;
            }

            Push(velocityDirection, speed);
        }

        public void Push(Vector2 velocityDirection, float speed)
        {
            ApplyAcceleration(velocityDirection * speed);
        }

        public void StopMoving()
        {
            Stop();
        }
    }
}
