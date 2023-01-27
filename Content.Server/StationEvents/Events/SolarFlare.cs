using JetBrains.Annotations;
using Content.Server.Radio.EntitySystems;

namespace Content.Server.StationEvents.Events
{
    public sealed class SolarFlare : StationEventSystem 
    {
        [Dependency] private readonly HeadsetSystem _headsetSystem = default!; 

        public override string Prototype => "SolarFlare";
        private float _endAfter = 0.0f;
        private const string affectedChannel = "Common";

        public override void Added()
        {
            base.Added();
            _endAfter = RobustRandom.Next(120, 240);
        }

        public override void Started()
        {
            base.Started();
            _headsetSystem.JamChannel(affectedChannel);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!RuleStarted)
                return;

            if (Elapsed > _endAfter)
            {
                ForceEndSelf();
                return;
            }
        }

        public override void Ended() {
            base.Ended();
            _headsetSystem.UnJamChannel(affectedChannel);
        }
    }
}