#nullable enable
using System;
using JetBrains.Annotations;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    [UsedImplicitly]
    public class StomachComponent : MechanismComponent
    {
        private float _accumulatedFrameTime;

        protected override Type? Network => typeof(DigestiveNetwork);

        public override void PreMetabolism(float frameTime)
        {
            base.PreMetabolism(frameTime);

            if (Mechanism.Body == null ||
                !Mechanism.Body.Owner.TryGetComponent(out StomachComponent? stomach))
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
