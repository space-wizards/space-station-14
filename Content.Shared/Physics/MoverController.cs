#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class MoverController : VirtualController
    {
        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        public void Move(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent == null)
                return;

            if (ControlledComponent.Owner.IsWeightless())
                return;

            if (!ControlledComponent.OnGround)
                return;

            Push(velocityDirection, speed);
        }

        public void Push(Vector2 velocityDirection, float speed)
        {
            if (ControlledComponent == null) return;
            if (ControlledComponent.LinearVelocity.Length > speed) return;
            ControlledComponent.Force += velocityDirection * speed * ControlledComponent.Mass;
        }

        public void StopMoving()
        {
            if (ControlledComponent == null || !ControlledComponent.OnGround) return;
            ControlledComponent.Force += -ControlledComponent.LinearVelocity * ControlledComponent.Mass;
        }
    }
}
