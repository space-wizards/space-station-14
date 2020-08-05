using Content.Server.Body.Network;
using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.Components.Body.Circulatory;
using JetBrains.Annotations;

namespace Content.Server.Body.Mechanisms.Behaviors
{
    [UsedImplicitly]
    public class HeartBehavior : MechanismBehavior
    {
        private float _accumulatedFrameTime;

        protected override void OnRemove()
        {
            Mechanism.Body?.RemoveNetwork<CirculatoryNetwork>();
        }

        public override void InstalledIntoBody()
        {
            Mechanism.Body?.EnsureNetwork<CirculatoryNetwork>();
        }

        public override void RemovedFromBody(BodyManagerComponent old)
        {
            old.RemoveNetwork<CirculatoryNetwork>();
        }

        public override void InstalledIntoPart()
        {
            Mechanism.Part?.Body?.EnsureNetwork<CirculatoryNetwork>();
        }

        public override void RemovedFromPart(BodyPart old)
        {
            old.Body?.RemoveNetwork<CirculatoryNetwork>();
        }

        public override void Update(float frameTime)
        {
            if (Mechanism.Body == null ||
                !Mechanism.Body.Owner.TryGetComponent(out BloodstreamComponent bloodstream))
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
