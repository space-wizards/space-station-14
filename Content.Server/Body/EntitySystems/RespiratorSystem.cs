using Content.Server.Body.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.EntitySystems
{
    [UsedImplicitly]
    public class RespiratorSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var respirator in ComponentManager.EntityQuery<RespiratorComponent>(false))
            {
                respirator.Update(frameTime);
            }
        }
    }
}
