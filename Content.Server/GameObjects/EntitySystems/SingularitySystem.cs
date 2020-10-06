using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Singularity;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.EntitySystems
{
    public class SingularitySystem : EntitySystem
    {
        private int tick;
        float curTimeSingulo;
        float curTimeGen;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            curTimeSingulo += frameTime;
            curTimeGen += frameTime;

            tick++;


            if (curTimeSingulo >= 1f)
            {
                curTimeSingulo -= 1f;
                foreach (var singulo in ComponentManager.EntityQuery<SingularityComponent>())
                {
                    singulo.Update();
                }
            }

            if (curTimeGen >= 3f)
            {
                curTimeGen -= 3f;
                foreach (var containment in ComponentManager.EntityQuery<ContainmentFieldGeneratorComponent>())
                {
                    containment.Update();
                }
            }
        }
    }
}
