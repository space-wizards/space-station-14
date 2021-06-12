using Content.Server.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.EntitySystems
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
