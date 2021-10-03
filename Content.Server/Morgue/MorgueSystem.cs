using Content.Server.Morgue.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Morgue
{
    [UsedImplicitly]
    public class MorgueSystem : EntitySystem
    {

        private float _accumulatedFrameTime;

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime >= 10)
            {
                foreach (var morgue in EntityManager.EntityQuery<MorgueEntityStorageComponent>(true))
                {
                    morgue.Update();
                }
                _accumulatedFrameTime -= 10;
            }
        }
    }
}
