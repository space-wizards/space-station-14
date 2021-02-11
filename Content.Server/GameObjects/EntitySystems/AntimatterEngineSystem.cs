using Content.Server.GameObjects.Components.Power.AME;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AntimatterEngineSystem : EntitySystem
    {
        private float _accumulatedFrameTime;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime >= 10)
            {
                foreach (var comp in ComponentManager.EntityQuery<AMEControllerComponent>(true))
                {
                    comp.OnUpdate(frameTime);
                }
                _accumulatedFrameTime -= 10;
            }

        }
    }
}
