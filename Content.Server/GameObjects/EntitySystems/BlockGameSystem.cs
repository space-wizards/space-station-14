using Content.Server.GameObjects.Components.Arcade;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class BlockGameSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<BlockGameArcadeComponent>())
            {
                comp.DoGameTick(frameTime);
            }
        }
    }
}
