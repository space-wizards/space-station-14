using Content.Server.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
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
