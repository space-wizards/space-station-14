using Content.Server.GameObjects.Components.Singularity;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

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
            var singulos = ComponentManager.EntityQuery<SingularityComponent>();
            foreach (var singulo in singulos)
            {
                singulo.PullUpdate();
                singulo.FrameUpdate(frameTime);
            }
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
