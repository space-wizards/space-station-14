using Content.Server.GameObjects.Components.Chemistry;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class VaporSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var vaporComp in ComponentManager.EntityQuery<VaporComponent>())
            {
                vaporComp.Update(frameTime);
            }
        }
    }
}
