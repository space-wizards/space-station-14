using System;
using Content.Server.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class StressTestMovementSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery<StressTestMovementComponent>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in RelevantEntities)
            {
                var stressTest = entity.GetComponent<StressTestMovementComponent>();
                var transform = entity.Transform;

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
