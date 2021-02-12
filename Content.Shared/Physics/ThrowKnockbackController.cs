using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;

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

            var broadPhase = EntitySystem.Get<SharedBroadPhaseSystem>();

            if (ControlledComponent.Owner.IsWeightless())
            {
                if (ActionBlockerSystem.CanMove(ControlledComponent.Owner)
                    && broadPhase.IsColliding(ControlledComponent, Vector2.Zero, false))
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
