using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.Components.Body.Networks;

namespace Content.Server.GameObjects.Components.Body.Behavior
{
    public class HeartBehavior : MechanismBehavior
    {
        private float _accumulatedFrameTime;

        public override void Update(float frameTime)
        {
            // TODO BODY do between pre and metabolism
            if (Parent.Body == null ||
                !Parent.Body.Owner.HasComponent<SharedBloodstreamComponent>())
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
