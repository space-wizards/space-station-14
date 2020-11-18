using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class ThrowKnockbackController : VirtualController
    {
        public ThrowKnockbackController()
        {
            IoCManager.InjectDependencies(this);
        }

        public void Push(Vector2 velocityDirection, float speed)
        {
            LinearVelocity = velocityDirection * speed;
        }

        private float Decay { get; set; } = 0.95f;

        public override void UpdateAfterProcessing()
        {
            if (ControlledComponent == null)
            {
                return;
            }

            if (ControlledComponent.Owner.IsWeightless())
            {
                if (ActionBlockerSystem.CanMove(ControlledComponent.Owner)
                    && ControlledComponent.IsColliding(Vector2.Zero, false))
                {
                    Stop();
                }

                return;
            }

            LinearVelocity *= Decay;

            if (LinearVelocity.Length < 0.001)
            {
                Stop();
            }
        }
    }
}
