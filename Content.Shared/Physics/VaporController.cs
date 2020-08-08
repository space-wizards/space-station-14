using Robust.Shared.Maths;
using Robust.Shared.Physics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.Physics
{
    public class VaporController : VirtualController
    {
        public void Move(Vector2 velocityDirection, float speed)
        {
            LinearVelocity = velocityDirection * speed;
        }
    }
}
