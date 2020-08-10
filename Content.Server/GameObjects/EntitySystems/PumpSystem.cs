using Content.Server.GameObjects.Components.Atmos;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// Placeholder for updating pipenet stuff
    /// </summary>
    public sealed class PumpSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var pump in ComponentManager.EntityQuery<BasePumpComponent>())
            {
                pump.Update(frameTime);
            }
            foreach (var vent in ComponentManager.EntityQuery<BaseVentComponent>())
            {
                vent.Update(frameTime);
            }
            foreach (var scrubber in ComponentManager.EntityQuery<BaseScrubberComponent>())
            {
                scrubber.Update(frameTime);
            }
        }
    }
}
