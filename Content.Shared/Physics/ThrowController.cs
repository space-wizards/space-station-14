using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Timers;

namespace Content.Shared.Physics
{
    public class ThrowController: VirtualController
    {
        private float _throwTime;
        private SharedPhysicsComponent _component;

        private const float DefaultThrowTime = 0.25f;

        public float ThrowTime
        {
            get => _throwTime;
            set => _throwTime = value;
        }

        public override SharedPhysicsComponent ControlledComponent
        {
            set => _component = value;
        }

        public void StartThrow()
        {
            _component.Status = BodyStatus.InAir;
            Timer.Spawn((int) (ThrowTime * 1000), StopThrow);
        }

        public void StopThrow()
        {
            _component.Status = BodyStatus.OnGround;
            _component.LinearVelocity = Vector2.Zero;
        }

        public ThrowController()
        {
            ThrowTime = DefaultThrowTime;
        }
    }
}
