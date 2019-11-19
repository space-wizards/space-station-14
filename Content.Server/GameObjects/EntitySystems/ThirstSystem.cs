using Content.Server.GameObjects.Components.Nutrition;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class ThirstSystem : EntitySystem
    {
        private float _accumulatedFrameTime;
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(ThirstComponent));
        }

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime > 1.0f)
            {
                foreach (var entity in RelevantEntities)
                {
                    var comp = entity.GetComponent<ThirstComponent>();
                    comp.OnUpdate(_accumulatedFrameTime);
                }
                _accumulatedFrameTime = 0.0f;
            }
        }
    }
}
