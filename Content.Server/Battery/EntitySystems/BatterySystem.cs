#nullable enable
using Content.Server.Battery.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Battery.EntitySystems
{
    [UsedImplicitly]
    public class BatterySystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<BatteryComponent>(true))
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
