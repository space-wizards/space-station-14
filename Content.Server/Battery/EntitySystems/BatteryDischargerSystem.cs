#nullable enable
using Content.Server.Power.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Battery.EntitySystems
{
    [UsedImplicitly]
    internal sealed class BatteryDischargerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<BatteryDischargerComponent>(false))
            {
                comp.Update(frameTime);
            }
        }
    }
}
