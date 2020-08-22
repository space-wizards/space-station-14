#nullable enable
using System;
using Content.Server.Body.Network;
using Content.Server.GameObjects.Components.Body.Digestive;
using JetBrains.Annotations;

namespace Content.Server.Body.Mechanisms.Behaviors
{
    [UsedImplicitly]
    public class StomachBehavior : MechanismBehavior
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
