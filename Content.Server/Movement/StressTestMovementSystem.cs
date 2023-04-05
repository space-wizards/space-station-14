using Content.Server.Movement.Components;
using JetBrains.Annotations;

namespace Content.Server.Movement
{
    [UsedImplicitly]
    internal sealed class StressTestMovementSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var stressTest in EntityManager.EntityQuery<StressTestMovementComponent>(true))
            {
                var transform = EntityManager.GetComponent<TransformComponent>(stressTest.Owner);

                stressTest.Progress += frameTime;

                if (stressTest.Progress > 1)
                {
                    stressTest.Progress -= 1;
                }

                var x = MathF.Sin(stressTest.Progress * MathHelper.TwoPi);
                var y = MathF.Cos(stressTest.Progress * MathHelper.TwoPi);

                transform.WorldPosition = stressTest.Origin + (new Vector2(x, y) * 5);
            }
        }
    }
}
