using Content.Server.GameObjects.Components.Nutrition;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class StomachSystem : EntitySystem
    {
        private float _accumulatedFrameTime;
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(StomachComponent));
        }

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;
            // TODO: Potential performance improvement (e.g. going through say 1/5th the entities every tick)
            if (_accumulatedFrameTime > 1.0f)
            {
                foreach (var entity in RelevantEntities)
                {
                    var comp = entity.GetComponent<StomachComponent>();
                    comp.OnUpdate(_accumulatedFrameTime);
                    _accumulatedFrameTime = 0.0f;
                }
            }
        }
    }
}
