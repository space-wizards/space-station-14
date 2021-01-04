using Content.Server.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SmokeSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var smokeComp in ComponentManager.EntityQuery<SmokeComponent>())
            {
                if (!smokeComp.IsInception)
                    smokeComp.Update(frameTime);
            }

            foreach (var smokeComp in ComponentManager.EntityQuery<SmokeComponent>())
            {
                if (smokeComp.IsInception)
                    smokeComp.Update(frameTime);
            }
        }
    }
}
