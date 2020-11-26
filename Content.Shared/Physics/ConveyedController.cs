#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Random;

namespace Content.Shared.Physics
{
    public class ConveyedController : VirtualController
    {
        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        public void Move(Vector2 velocityDirection, float speed, Vector2 itemRelativeToConveyor)
        {
            if (ControlledComponent?.Owner.IsWeightless() ?? false)
            {
                return;
            }

            if (ControlledComponent?.Status == BodyStatus.InAir)
            {
                return;
            }

            LinearVelocity = velocityDirection * speed;

            //gravitating item towards center
            //http://csharphelper.com/blog/2016/09/find-the-shortest-distance-between-a-point-and-a-line-segment-in-c/
            Vector2 centerPoint;

            var t = 0f;
            if (velocityDirection.Length > 0) //if velocitydirection is 0, this calculation will divide by 0
            {
                t = Vector2.Dot(itemRelativeToConveyor, velocityDirection) /
                    Vector2.Dot(velocityDirection, velocityDirection);
            }

            if (t < 0)
            {
                centerPoint = new Vector2();
            }
            else if(t > 1)
            {
                centerPoint = velocityDirection;
            }
            else
            {
                centerPoint = velocityDirection * t;
            }

            var delta = centerPoint - itemRelativeToConveyor;
            LinearVelocity += delta * (4 * delta.Length);
        }

        public override void UpdateAfterProcessing()
        {
            base.UpdateAfterProcessing();

            LinearVelocity = Vector2.Zero;
        }
    }
}
