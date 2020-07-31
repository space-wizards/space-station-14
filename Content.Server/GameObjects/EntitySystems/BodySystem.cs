using Content.Server.GameObjects.Components.Body;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class BodySystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var body in ComponentManager.EntityQuery<BodyManagerComponent>())
            {
                // TODO
                body.Update(frameTime);
            }
        }
    }
}
