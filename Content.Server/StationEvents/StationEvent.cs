using Content.Server.Interfaces.Chat;
using Robust.Shared.GameObjects.Systems;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.IoC;

namespace Content.Server.StationEvents
{
    public abstract class StationEvent
    {
        /// <summary>
        /// If the event has started and is currently running.
        /// </summary>
        public bool Running { get; protected set; }
        
        /// <summary>
        /// Human-readable name for the event.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The weight this event has in the random-selection process.
        /// </summary>
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
        /// Starting audio of the event.
        /// </summary>
        protected virtual string StartAudio { get; } = "/Audio/Effects/alert.ogg";

        /// <summary>
        /// Ending audio of the event.
        /// </summary>
        protected virtual string EndAudio { get; } = null;

        /// <summary>
        /// Can the false alarm fake this event?
        /// </summary>
        public virtual bool Fakeable { get; } = true;

        /// <summary>
        /// In minutes, when is the first time this event can start
        /// </summary>
        public virtual int EarliestStart { get; } = 5;

        /// <summary>
        /// When in the lifetime to call Start().
        /// </summary>
        protected virtual float StartWhen { get; } = 0.0f;

        /// <summary>
        /// When in the lifetime to call Announce().
        /// </summary>
        /// this has a set for dynamic configuration.
        protected virtual float AnnounceWhen { get; set; } = 0.0f;

        /// <summary>
        /// When in the lifetime the event should end.
        /// </summary>
        protected virtual float EndWhen { get; set; } = 0.0f;

        /// <summary>
        /// How long has the event existed. Do not change this.
        /// </summary>
        public virtual float ActiveFor { get; protected set; } = 0.0f;

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

        private bool Started { get; set; } = false;
        private bool Announced { get; set; } = false;
        // No "Ended" as this assumes the end properly kicks off.

        /// <summary>
        /// Called before Start(). Allows you to setup your events, such as randomly setting variables.
        /// </summary>
        public virtual void Setup()
        {
            Running = true;
            Occurrences += 1;
        }

        /// <summary>
        /// Called when the tick is equal to the StartWhen variable.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Called when the tick is qual to the AnnounceWhen variable.
        /// </summary>
        /// <param name="fake"></param>
        public virtual void Announce(bool fake)
        {
            if (StartAnnouncement != null)
            {
                var chatManager = IoCManager.Resolve<IChatManager>();
                chatManager.DispatchStationAnnouncement(StartAnnouncement);
            }
            if (StartAudio != null)
            {
                EntitySystem.Get<AudioSystem>().PlayGlobal(StartAudio, AudioParams.Default.WithVolume(-10f));
            }
        }

        /// <summary>
        /// Called once when the station event ends
        /// </summary>
        public virtual void End()
        {
            if (EndAnnouncement != null)
            {
                var chatManager = IoCManager.Resolve<IChatManager>();
                chatManager.DispatchStationAnnouncement(EndAnnouncement);
            }
            if (EndAudio != null)
            {
                EntitySystem.Get<AudioSystem>().PlayGlobal(EndAudio, AudioParams.Default.WithVolume(-10f));
            }
            Running = false;
        }

        /// <summary>
        /// Called every tick when this event is active
        /// </summary>
        /// <param name="frameTime"></param>
        public virtual void Tick(float frameTime)
        {
            return;
        }

        /// <summary>
        /// Called every tick when this event is running. Do not override me!
        /// </summary>
        /// <param name="frameTime"></param>
        public virtual void Update(float frameTime)
        {
            if (!Running)
            {
                return;
            }

            if (ActiveFor >= StartWhen && !Started)
            {
                Start();
                Started = true;
            }

            if (ActiveFor >= AnnounceWhen && !Announced)
            {
                Announce(false);
                Announced = true;
            }

            if (StartWhen < ActiveFor && ActiveFor < EndWhen)
            {
                Tick(frameTime);
            }

            if (ActiveFor >= EndWhen && ActiveFor >= AnnounceWhen && ActiveFor >= StartWhen)
            {
                End();
                Started = false;
                Announced = false;
                return;
            }
            ActiveFor += frameTime;
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
