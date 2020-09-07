using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class SlipController : VirtualController
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
