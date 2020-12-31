using Robust.Shared.Maths;
using Robust.Shared.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Physics
{
    public class JetpackController : VirtualController
    {
        public void Push(Vector2 velocityDirection, float speed)
        {
            LinearVelocity = velocityDirection * speed;
        }
    }
}
