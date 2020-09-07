#nullable enable
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class BulletController : VirtualController
    {
        public void Push(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent != null)
            {
                ControlledComponent.Force += velocityDirection * speed;
            }
        }
    }
}
