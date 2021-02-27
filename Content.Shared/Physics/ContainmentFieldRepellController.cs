#nullable enable
using Robust.Shared.Maths;

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
