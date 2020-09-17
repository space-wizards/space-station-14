using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class LungSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesBefore.Add(typeof(SharedMetabolismSystem));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var lung in ComponentManager.EntityQuery<SharedLungBehaviorComponent>())
            {
                lung.Update(frameTime);
            }
        }
    }
}
