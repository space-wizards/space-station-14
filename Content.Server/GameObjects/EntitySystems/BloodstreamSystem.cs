using Content.Server.GameObjects.Components.Metabolism;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// Triggers metabolism updates for <see cref="BloodstreamComponent"/>
    /// </summary>
    [UsedImplicitly]
    public class BloodstreamSystem : EntitySystem
    {
        private float _accumulatedFrameTime;
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(BloodstreamComponent));
        }

        public override void Update(float frameTime)
        {
            //Trigger metabolism updates at most once per second
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime > 1.0f)
            {
                foreach (var entity in RelevantEntities)
                {
                    var comp = entity.GetComponent<BloodstreamComponent>();
                    comp.OnUpdate(_accumulatedFrameTime);
                }
                _accumulatedFrameTime = 0.0f;
            }
        }
    }
}
