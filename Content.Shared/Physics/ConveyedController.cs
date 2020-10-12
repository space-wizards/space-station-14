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
        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        public void Move(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent?.Owner.HasComponent<MovementIgnoreGravityComponent>() == false &&
                IoCManager.Resolve<IPhysicsManager>().IsWeightless(ControlledComponent.Owner.Transform.Coordinates))
            {
                return;
            }

            if (ControlledComponent?.Status == BodyStatus.InAir)
            {
                return;
            }

            LinearVelocity = velocityDirection * speed * 100;
        }

        public override void UpdateAfterProcessing()
        {
            base.UpdateAfterProcessing();

            LinearVelocity = Vector2.Zero;
        }
    }
}
