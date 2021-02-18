using Content.Server.GameObjects.Components.Singularity;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SingularitySystem : EntitySystem
    {
        private float curTimeSingulo;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            curTimeSingulo += frameTime;
<<<<<<< HEAD
            var singulos = ComponentManager.EntityQuery<SingularityComponent>();
            foreach (var singulo in singulos)
            {
                singulo.PullUpdate();
                singulo.FrameUpdate(frameTime);
            }
=======
            curTimePull += frameTime;

            var shouldUpdate = curTimeSingulo >= 1f;
            var shouldPull = curTimePull >= 0.2f;
            if (!shouldUpdate && !shouldPull) return;
            var singulos = ComponentManager.EntityQuery<SingularityComponent>(true);

>>>>>>> 8640f342b5444c9209d41af53bb00180e2f3896e
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
