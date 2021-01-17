using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;

namespace Content.Shared.Physics
{
    public class SlipController : VirtualController
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        public SlipController()
        {
            IoCManager.InjectDependencies(this);
        }

        private float Decay { get; set; } = 0.95f;

        public override void UpdateAfterProcessing()
        {
            if (ControlledComponent == null)
            {
                return;
            }

            var broadPhase = EntitySystem.Get<SharedBroadPhaseSystem>();

            if (_physicsManager.IsWeightless(ControlledComponent.Owner.Transform.Coordinates))
            {
                if (broadPhase.IsColliding(ControlledComponent, Vector2.Zero, false))
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
