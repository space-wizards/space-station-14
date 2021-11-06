using Content.Server.AME.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.AME
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
                foreach (var comp in EntityManager.EntityQuery<AMEControllerComponent>())
                {
                    comp.OnUpdate(frameTime);
                }
                _accumulatedFrameTime -= 10;
            }

        }
    }
}
