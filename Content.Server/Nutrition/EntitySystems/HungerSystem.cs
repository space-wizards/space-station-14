using Content.Server.GameObjects.Components.Nutrition;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
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
                foreach (var comp in ComponentManager.EntityQuery<HungerComponent>(true))
                {
                    comp.OnUpdate(_accumulatedFrameTime);
                }
                _accumulatedFrameTime -= 1;
            }
        }
    }
}
