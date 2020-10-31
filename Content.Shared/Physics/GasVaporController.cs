using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class GasVaporController : VirtualController
    {
        public void Move(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent != null)
            {
                ControlledComponent.Force += velocityDirection * speed;
            }
        }
    }
}
