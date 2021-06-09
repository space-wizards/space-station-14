using Content.Server.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class VaporSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var vaporComp in ComponentManager.EntityQuery<VaporComponent>(true))
            {
                vaporComp.Update(frameTime);
            }
        }
    }
}
