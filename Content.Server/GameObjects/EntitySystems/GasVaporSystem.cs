using Content.Server.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class VaporSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var GasVapor in ComponentManager.EntityQuery<GasVaporComponent>())
            {
                GasVapor.Update();
            }
        }
    }
}
