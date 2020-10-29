using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class ContainmentFieldRepellController : FrictionController
    {
        public void Repell(Direction dir, float speed)
        {
            LinearVelocity = dir.ToVec() * speed;
        }
    }
}
