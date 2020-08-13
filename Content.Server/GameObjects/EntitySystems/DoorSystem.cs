using Content.Server.GameObjects.Components.Doors;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    class DoorSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<ServerDoorComponent>())
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
