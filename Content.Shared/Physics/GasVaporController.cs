using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class GasVaporController : VirtualController
    {
        public void Move(Vector2 velocityDirection, float speed)
        {
            LinearVelocity = velocityDirection * speed;
        }
    }
}
