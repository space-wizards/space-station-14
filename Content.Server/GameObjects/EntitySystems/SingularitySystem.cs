using Content.Server.GameObjects.Components.Singularity;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SingularitySystem : EntitySystem
    {
        private float curTimeSingulo, curTimePull;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            curTimeSingulo += frameTime;
            curTimePull += frameTime;

            var shouldUpdate = curTimeSingulo >= 1f;
            var shouldPull = curTimePull >= 0.2f;
            if (!shouldUpdate && !shouldPull) return;
            var singulos = ComponentManager.EntityQuery<SingularityComponent>(true);

            if (curTimeSingulo >= 1f)
            {
                curTimeSingulo -= 1f;
                foreach (var singulo in singulos)
                {
                    singulo.Update();
                }
            }

        }
    }
}
