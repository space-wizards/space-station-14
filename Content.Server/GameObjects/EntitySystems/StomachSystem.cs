using Content.Server.GameObjects.Components.Nutrition;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    /// <summary>
    /// Triggers digestion updates on <see cref="StomachComponent"/>
    /// </summary>
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
            //Update at most once per second
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime > 1.0f)
            {
                foreach (var entity in RelevantEntities)
                {
                    var comp = entity.GetComponent<StomachComponent>();
                    comp.OnUpdate(_accumulatedFrameTime);
                }
                _accumulatedFrameTime = 0.0f;
            }
        }
    }
}
