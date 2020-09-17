using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class HeartSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesBefore.Add(typeof(SharedMetabolismSystem));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var heart in ComponentManager.EntityQuery<SharedHeartBehaviorComponent>())
            {
                heart.Update(frameTime);
            }
        }
    }
}
