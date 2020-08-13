using Content.Server.GameObjects.Components.Nutrition;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    ///     Triggers digestion updates on <see cref="StomachComponent"/>
    /// </summary>
    [UsedImplicitly]
    internal sealed class StomachSystem : EntitySystem
    {
        private float _accumulatedFrameTime;

        public override void Update(float frameTime)
        {
            //Update at most once per second
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime > 1.0f)
            {
                foreach (var component in ComponentManager.EntityQuery<StomachComponent>())
                {
                    component.OnUpdate(_accumulatedFrameTime);
                }
                _accumulatedFrameTime -= 1.0f;
            }
        }
    }
}
