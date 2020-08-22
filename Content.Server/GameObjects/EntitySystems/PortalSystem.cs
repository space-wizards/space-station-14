using Content.Server.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class PortalSystem : EntitySystem
    {
        // TODO: Someone refactor portals
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<ServerPortalComponent>())
            {
                comp.OnUpdate();
            }
        }
    }
}
