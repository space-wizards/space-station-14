#nullable enable
using JetBrains.Annotations;
using Content.Server.GameObjects.EntitySystems.StationEvents;
using Robust.Shared.GameObjects;

namespace Content.Server.StationEvents
{
    [UsedImplicitly]
    public sealed class FalseAlarm : StationEvent
    {
        public override string Name => "FalseAlarm";
        public override float Weight => WeightHigh;
        protected override float EndAfter => 1.0f;
        public override int? MaxOccurrences => 5;

        public override void Announce()
        {
            var stationEventSystem = EntitySystem.Get<StationEventSystem>();
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
