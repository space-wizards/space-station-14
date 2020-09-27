using Content.Server.Interfaces.Chat;
using Robust.Shared.IoC;

namespace Content.Server.StationEvents
{
    public abstract class StationEvent
    {
        /// <summary>
        /// If the event has started and is currently running
        /// </summary>
        public bool Running { get; protected set; }
        
        /// <summary>
        /// Human-readable name for the event
        /// </summary>
        public abstract string Name { get; }

        public virtual StationEventWeight Weight { get; } = StationEventWeight.Normal;

        /// <summary>
        /// What should be said in chat when the event starts (if anything).
        /// </summary>
        protected virtual string StartAnnouncement { get; } = null;

        /// <summary>
        /// What should be said in chat when the event end (if anything).
        /// </summary>
        protected virtual string EndAnnouncement { get; } = null;

        /// <summary>
        /// In minutes, when is the first time this event can start
        /// </summary>
        /// <returns></returns>
        public virtual int EarliestStart { get; } = 20;

        /// <summary>
        /// How many players need to be present on station for the event to run
        /// </summary>
        /// To avoid running deadly events with low-pop
        public virtual int MinimumPlayers { get; } = 0;

        /// <summary>
        /// How many times this event has run this round
        /// </summary>
        public int Occurrences { get; set; } = 0;

        /// <summary>
        /// How many times this even can occur in a single round
        /// </summary>
        public virtual int? MaxOccurrences { get; } = null;

        /// <summary>
        /// Called once when the station event starts
        /// </summary>
        public virtual void Startup()
        {
            Running = true;
            Occurrences += 1;
            if (StartAnnouncement != null)
            {
                var chatManager = IoCManager.Resolve<IChatManager>();
                chatManager.DispatchStationAnnouncement(StartAnnouncement);
            }
        }

        /// <summary>
        /// Called every tick when this event is active
        /// </summary>
        /// <param name="frameTime"></param>
        public abstract void Update(float frameTime);

        /// <summary>
        /// Called once when the station event ends
        /// </summary>
        public virtual void Shutdown()
        {
            if (EndAnnouncement != null)
            {
                var chatManager = IoCManager.Resolve<IChatManager>();
                chatManager.DispatchStationAnnouncement(EndAnnouncement);
            }
        }
    }

    public enum StationEventWeight
    {
        VeryLow = 0,
        Low = 5,
        Normal = 10,
        High = 15,
        VeryHigh = 20,
    }
}