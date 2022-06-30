using JetBrains.Annotations;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed class FalseAlarm : StationEventSystem
    {
        public override string Name => "FalseAlarm";
        public override float Weight => WeightHigh;
        protected override float EndAfter => 1.0f;
        public override int? MaxOccurrences => 5;

        public override void Announce()
        {
            var stationEventSystem = EntitySystem.Get<StationEventSchedulerSystem>();
            var randomEvent = stationEventSystem.PickRandomEvent();

            if (randomEvent != null)
            {
                StartAnnouncement = randomEvent.StartAnnouncement;
                StartAudio = randomEvent.StartAudio;
            }

            base.Announce();
        }
    }
}
