using Content.Server.Atmos;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class GasVaporSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var GasVapor in ComponentManager.EntityQuery<GasVaporComponent>())
            {
                if (GasVapor.Initialized)
                {
                    GasVapor.Update(frameTime);
                }
            }
        }
    }
}
