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

        public override void Update(float frameTime)
        {
            if (Mechanism.Body == null ||
                !Mechanism.Body.Owner.TryGetComponent(out StomachComponent bloodstream))
            {
                return;
            }

            // Update at most once per second
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime >= 1)
            {
                bloodstream.Update(_accumulatedFrameTime);
                _accumulatedFrameTime = 0;
            }
        }
    }
}
