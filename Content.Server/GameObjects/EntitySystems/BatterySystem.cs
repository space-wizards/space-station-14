using Content.Server.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class BatterySystem : EntitySystem
    {
        [Dependency] private readonly IPauseManager _pauseManager = default!;

        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<BatteryComponent>())
            {
                if (comp.Owner.Paused)
                {
                    continue;
                }

                comp.OnUpdate(frameTime);
            }
        }
    }
}
