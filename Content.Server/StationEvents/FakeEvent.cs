using JetBrains.Annotations;
using Content.Server.GameObjects.EntitySystems.StationEvents;
using Content.Server.Interfaces.Chat;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Audio;

namespace Content.Server.StationEvents
{
    [UsedImplicitly]
    public sealed class FakeEvent : StationEvent
    {
        public override string Name => "FakeEvent";
        
        /// <summary>
        /// Called once when the station event starts. We only need to announce it's message and we're done!
        /// </summary>
        public override void Startup()
        {
            Running = true;
            Occurrences += 1;

            var stationEventSystem = EntitySystem.Get<StationEventSystem>();
            var randomEvent = stationEventSystem.PickRandomEvent();

            if (randomEvent.StartAnnouncement != null)
            {
                EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Effects/alert.ogg", AudioParams.Default.WithVolume(-10f));
                var chatManager = IoCManager.Resolve<IChatManager>();
                chatManager.DispatchStationAnnouncement(randomEvent.StartAnnouncement);
            }

            Running = false;
        }
        public override void Update(float frameTime)
        {
            // no.
            return;
        }
    }
    
}
