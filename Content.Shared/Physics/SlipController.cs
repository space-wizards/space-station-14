using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public class SlipController : VirtualController
    {
#pragma warning disable 649
        [Dependency] private readonly IPhysicsManager _physicsManager;
#pragma warning restore 649

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

            if (_physicsManager.IsWeightless(ControlledComponent.Owner.Transform.GridPosition))
            {
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
