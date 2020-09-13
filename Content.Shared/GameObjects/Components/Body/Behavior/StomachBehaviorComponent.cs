#nullable enable
using System;
using JetBrains.Annotations;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    [UsedImplicitly]
    public class StomachBehaviorComponent : MechanismBehaviorComponent
    {
        public override string Name => "Stomach";

        private float _accumulatedFrameTime;

        public override void Update(float frameTime)
        {
            if (Mechanism?.Body == null ||
                !Mechanism.Body.Owner.TryGetComponent(out StomachBehaviorComponent? stomach))
            {
                return;
            }

            // Update at most once per second
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime >= 1)
            {
                stomach.Update(_accumulatedFrameTime);
                _accumulatedFrameTime -= 1;
            }
        }
    }
}
