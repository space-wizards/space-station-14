using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.Components.Metabolism;
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
                body.PreMetabolism(frameTime);
            }

            foreach (var metabolism in ComponentManager.EntityQuery<MetabolismComponent>())
            {
                metabolism.Update(frameTime);
            }

            foreach (var body in ComponentManager.EntityQuery<BodyManagerComponent>())
            {
                body.PostMetabolism(frameTime);
            }
        }
    }
}
