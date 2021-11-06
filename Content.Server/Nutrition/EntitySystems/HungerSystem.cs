using Content.Server.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public class HungerSystem : EntitySystem
    {
        private float _accumulatedFrameTime;

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime > 1)
            {
                foreach (var comp in EntityManager.EntityQuery<HungerComponent>())
                {
                    comp.OnUpdate(_accumulatedFrameTime);
                }
                _accumulatedFrameTime = 0;
            }
        }
    }
}
