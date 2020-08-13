using Content.Server.GameObjects.Components.Metabolism;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    /// <summary>
    ///     Triggers metabolism updates for <see cref="BloodstreamComponent"/>
    /// </summary>
    [UsedImplicitly]
    internal sealed class BloodstreamSystem : EntitySystem
    {
        private float _accumulatedFrameTime;

        public override void Update(float frameTime)
        {
            //Trigger metabolism updates at most once per second
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime > 1.0f)
            {
                foreach (var component in ComponentManager.EntityQuery<BloodstreamComponent>())
                {
                    component.OnUpdate(_accumulatedFrameTime);
                }
                _accumulatedFrameTime -= 1.0f;
            }
        }
    }
}
