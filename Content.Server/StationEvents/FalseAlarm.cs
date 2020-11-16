using JetBrains.Annotations;
using Content.Server.GameObjects.EntitySystems.StationEvents;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.StationEvents
{
    [UsedImplicitly]
    public sealed class FalseAlarm : StationEvent
    {
        public override string Name => "FalseAlarm";

        public override StationEventWeight Weight => StationEventWeight.High;

        public override bool Fakeable => false;

        protected override float EndWhen => 1.0f;

        public override int? MaxOccurrences => 5;

        /// <summary>
        /// Called once when the station event starts. We only need to announce it's message and we're done!
        /// </summary>
        public override void Start()
        {
            var stationEventSystem = EntitySystem.Get<StationEventSystem>();
            var randomEvent = stationEventSystem.PickRandomEvent();

            if (randomEvent.Fakeable)
            {
                randomEvent.Announce(true);
            }
        }
    }
    
}
