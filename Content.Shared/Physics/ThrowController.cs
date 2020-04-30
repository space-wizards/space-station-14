using Robust.Shared.Interfaces.Timers;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Timers;

namespace Content.Shared.Physics
{
    public class ThrowController: VirtualController
    {
        private float _throwTime;
        private SharedPhysicsComponent _controller;

        public float ThrowTime
        {
            get => _throwTime;
            set => _throwTime = value;
        }

        public override SharedPhysicsComponent ControlledComponent
        {
            set => _controller = value;
        }

        public void StartThrow()
        {
            Timer.Spawn((int) (ThrowTime * 1000), () =>
            {

            });
        }

        public override void UpdateBeforeProcessing()
        {
            base.UpdateBeforeProcessing();
        }
    }
}
