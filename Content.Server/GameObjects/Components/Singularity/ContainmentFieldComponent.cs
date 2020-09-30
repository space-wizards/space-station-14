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

            var pushForce = 1f;
            FrictionController frictionController;
            if (!collidableComponent.TryGetController<FrictionController>(out frictionController))
            {
                frictionController = collidableComponent.EnsureController<FrictionController>();
                pushForce = 1.2f;
            }

            if (Math.Abs(Owner.Transform.WorldRotation.Degrees + 90f) < 0.1f ||
                Math.Abs(Owner.Transform.WorldRotation.Degrees - 90f) < 0.1f)
            {
                frictionController.LinearVelocity = new Vector2(collidableComponent.LinearVelocity.X * -pushForce, collidableComponent.LinearVelocity.Y * pushForce);
            }
            else
            {
                frictionController.LinearVelocity = new Vector2(collidableComponent.LinearVelocity.X * pushForce, collidableComponent.LinearVelocity.Y * -pushForce);
            }
        }
    }
}
