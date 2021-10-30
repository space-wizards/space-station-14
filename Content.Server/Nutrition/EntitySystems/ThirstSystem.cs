using Content.Server.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public class ThirstSystem : EntitySystem
    {
        private float _accumulatedFrameTime;

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime > 1)
            {
                foreach (var component in EntityManager.EntityQuery<ThirstComponent>())
                {
                    component.OnUpdate(_accumulatedFrameTime);
                }
                _accumulatedFrameTime -= 1;
            }
        }
    }
}
