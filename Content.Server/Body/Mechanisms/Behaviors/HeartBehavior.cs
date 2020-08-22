#nullable enable
using System;
using Content.Server.Body.Network;
using Content.Server.GameObjects.Components.Body.Circulatory;
using JetBrains.Annotations;

namespace Content.Server.Body.Mechanisms.Behaviors
{
    [UsedImplicitly]
    public class HeartBehavior : MechanismBehavior
    {
        private float _accumulatedFrameTime;

        protected override Type? Network => typeof(CirculatoryNetwork);

        public override void PreMetabolism(float frameTime)
        {
            // TODO do between pre and metabolism
            base.PreMetabolism(frameTime);

            if (Mechanism.Body == null ||
                !Mechanism.Body.Owner.TryGetComponent(out BloodstreamComponent? bloodstream))
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
