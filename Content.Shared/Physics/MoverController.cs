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
        public override ICollidableComponent? ControlledComponent { protected get; set; }

        public void Move(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent?.Owner.HasComponent<MovementIgnoreGravityComponent>() == false
                && IoCManager.Resolve<IPhysicsManager>().IsWeightless(ControlledComponent.Owner.Transform.GridPosition))
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
