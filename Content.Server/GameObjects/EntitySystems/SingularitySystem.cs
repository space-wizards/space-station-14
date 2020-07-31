using Content.Server.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.EntitySystems
{
    public class SingularitySystem : EntitySystem
    {

        float curTime;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            curTime += frameTime;

            if (curTime >= 1f)
            {
                curTime = 0f;
                foreach (var singulo in ComponentManager.EntityQuery<SingularityComponent>()) {
                    singulo.Update();
                }
            }
        }
    }
}
