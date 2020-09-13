#nullable enable
using Content.Shared.GameObjects.Components.Body.Networks;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    [RegisterComponent]
    [ComponentReference(typeof(ISharedMechanismBehavior))]
    public class HeartComponent : MechanismComponent
    {
        public override string Name => "Heart";

        private float _accumulatedFrameTime;

        public void Update(float frameTime)
        {
            // TODO do between pre and metabolism
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
