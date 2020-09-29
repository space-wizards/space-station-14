#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class MoverController : VirtualController
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        public override ICollidableComponent? ControlledComponent { protected get; set; }

        public void Move(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent?.Owner.HasComponent<MovementIgnoreGravityComponent>() == false
                && _physicsManager.IsWeightless(ControlledComponent.Owner.Transform.Coordinates))
            {
                return;
            }

            Push(velocityDirection, speed);
        }

        public void Push(Vector2 velocityDirection, float speed)
        {
            LinearVelocity = velocityDirection * speed;
        }

        public void StopMoving()
        {
            LinearVelocity = Vector2.Zero;
        }
    }
}
