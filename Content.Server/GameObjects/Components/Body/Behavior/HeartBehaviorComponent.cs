using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.Components.Body.Networks;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Body.Behavior
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHeartBehaviorComponent))]
    public class HeartBehaviorComponent : SharedHeartBehaviorComponent
    {
        private float _accumulatedFrameTime;

        public override void Update(float frameTime)
        {
            // TODO BODY do between pre and metabolism
            if (Mechanism?.Body == null ||
                !Mechanism.Body.Owner.HasComponent<SharedBloodstreamComponent>())
            {
                return;
            }

            // Update at most once per second
            _accumulatedFrameTime += frameTime;

            // TODO: Move/accept/process bloodstream reagents only when the heart is pumping
            if (_accumulatedFrameTime >= 1)
            {
                // bloodstream.Update(_accumulatedFrameTime);
                _accumulatedFrameTime -= 1;
            }
        }
    }
}
