#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class ConveyedController : VirtualController
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        public void Move(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent == null)
            {
                return;
            }

            if (!ControlledComponent.Owner.HasComponent<MovementIgnoreGravityComponent>() &&
                _physicsManager.IsWeightless(ControlledComponent.Owner.Transform.Coordinates))
            {
                return;
            }

            if (ControlledComponent.Status == BodyStatus.InAir)
            {
                return;
            }

            ControlledComponent.Force += velocityDirection * speed * 100;
        }
    }
}
