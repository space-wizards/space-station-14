using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Recycling;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class GasCanisterSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var component in ComponentManager.EntityQuery<GasCanisterComponent>(true))
            {
                component.Update(frameTime);
            }
        }
    }
}
