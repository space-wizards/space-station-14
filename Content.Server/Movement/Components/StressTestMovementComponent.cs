using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.Movement.Components
{
    [RegisterComponent]
    public class StressTestMovementComponent : Component
    {
        public override string Name => "StressTestMovement";

        public float Progress { get; set; }
        public Vector2 Origin { get; set; }

        protected override void Startup()
        {
            base.Startup();

            Origin = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).WorldPosition;
        }
    }
}
