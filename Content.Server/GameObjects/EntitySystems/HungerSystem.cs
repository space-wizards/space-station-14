using Content.Server.GameObjects.Components.Nutrition;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class HungerSystem : EntitySystem
    {
        private float _accumulatedFrameTime;
        
        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime > 1.0f)
            {
                foreach (var comp in ComponentManager.EntityQuery<HungerComponent>())
                {
                    comp.OnUpdate(_accumulatedFrameTime);
                }
                _accumulatedFrameTime -= 1.0f;
            }
        }
    }
}
