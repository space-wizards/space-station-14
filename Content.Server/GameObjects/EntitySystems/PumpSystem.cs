using Content.Server.GameObjects.Components.Atmos;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class PumpSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var pump in ComponentManager.EntityQuery<BasePumpComponent>())
            {
                pump.Update(frameTime);
            }
        }
    }
}
