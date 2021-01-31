#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class MoverController : VirtualController
    {
        public void Move(Vector2 velocityDirection)
        {
            Impulse = velocityDirection;
        }

        public void StopMoving()
        {
            Impulse = Vector2.Zero;
        }
    }
}
