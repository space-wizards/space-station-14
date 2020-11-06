using Content.Server.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class BatterySystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<BatteryComponent>(false))
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
