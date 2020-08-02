using Content.Server.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.EntitySystems
{
    public class SingularitySystem : EntitySystem
    {
        private int tick;
        float curTime;
        public override void Update(float frameTime)
        {
            tick++;
            base.Update(frameTime);

            curTime += frameTime;

            if (curTime >= 1f)
            {
                curTime -= 1f;
                foreach (var singulo in ComponentManager.EntityQuery<SingularityComponent>())
                {
                    singulo.Update();
                }
            }

            if (tick == 4)
            {
                tick = 0;
                foreach (var singulo in ComponentManager.EntityQuery<SingularityComponent>())
                {
                    singulo.TileUpdate();
                }
            }
        }
    }
}
