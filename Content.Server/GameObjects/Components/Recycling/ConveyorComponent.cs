using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Recycling
{
    [RegisterComponent]
    public class ConveyorComponent : Component, ICollideBehavior
    {
        public override string Name => "Conveyor";

        public void CollideWith(IEntity collidedWith)
        {
            collidedWith.Transform.LocalPosition += Owner.Transform.LocalRotation.ToVec() / 10;
        }
    }
}
