using System;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ContainmentFieldComponent : Component, ICollideBehavior
    {
        public override string Name => "ContainmentField";

        public void CollideWith(IEntity collidedWith)
        {
            if (!collidedWith.TryGetComponent<ICollidableComponent>(out var collidableComponent)) return;

            var speed = 5;
            var containmentFieldRepellController = collidableComponent.EnsureController<ContainmentFieldRepellController>();
            //if(collidedWith.TryGetComponent<SingularityComponent>())

            if (Math.Abs(Owner.Transform.WorldRotation.Degrees + 90f) < 0.1f ||
                Math.Abs(Owner.Transform.WorldRotation.Degrees - 90f) < 0.1f)
            {
                if (Owner.Transform.WorldPosition.X.CompareTo(collidedWith.Transform.WorldPosition.X) > 0)
                {
                    containmentFieldRepellController.Repell(Direction.West, speed);
                }
                else
                {
                    containmentFieldRepellController.Repell(Direction.East, speed);
                }
            }
            else
            {
                if (Owner.Transform.WorldPosition.Y.CompareTo(collidedWith.Transform.WorldPosition.Y) > 0)
                {
                    containmentFieldRepellController.Repell(Direction.South, speed);
                }
                else
                {
                    containmentFieldRepellController.Repell(Direction.North, speed);
                }
            }
        }
    }
}
