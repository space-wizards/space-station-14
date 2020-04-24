using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class PullableComponent: Component
    {
        private const float DistBeforePull = 0.25f;

        private const float PullStrength = 5.0f;

        private PhysicsComponent _physicsComponent;

        private IEntity _puller;
        public override string Name => "Pullable";

        public override void Initialize()
        {
            base.Initialize();

            _physicsComponent = Owner.GetComponent<PhysicsComponent>();

            _puller = null;
        }

        public void GetPulled(IEntity puller)
        {
            _puller = puller;
        }

        public void StopPull()
        {
            _puller = null;
        }

        public void Update()
        {
            if (_puller == null) return;

            // Are we outside of pulling range?
            var dist = Owner.Transform.WorldPosition - _puller.Transform.WorldPosition;
            if (dist.Length > DistBeforePull)
            {
                _physicsComponent.LinearVelocity = dist.Normalized * PullStrength;
            }
        }
    }
}
