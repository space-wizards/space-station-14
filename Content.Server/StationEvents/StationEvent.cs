#nullable enable
using Content.Server.Interfaces.Chat;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.StationEvents
{
    public abstract class StationEvent
    {
        public const float WeightVeryLow = 0.0f;
        public const float WeightLow = 5.0f;
        public const float WeightNormal = 10.0f;
        public const float WeightHigh = 15.0f;
        public const float WeightVeryHigh = 20.0f;

        /// <summary>
        ///     If the event has started and is currently running.
        /// </summary>
        public bool Running { get; set; }

        /// <summary>
        ///     Human-readable name for the event.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The weight this event has in the random-selection process.
        /// </summary>
        public virtual float Weight => WeightNormal;

        /// <summary>
        ///     What should be said in chat when the event starts (if anything).
        /// </summary>
        public virtual string? StartAnnouncement { get; set; } = null;

        /// <summary>
        ///     What should be said in chat when the event ends (if anything).
        /// </summary>
        protected virtual string? EndAnnouncement { get; } = null;

        /// <summary>
        ///     Starting audio of the event.
        /// </summary>
        public virtual string? StartAudio { get; set; } = null;

        /// <summary>
        ///     Ending audio of the event.
        /// </summary>
        public virtual string? EndAudio { get; } = null;

        /// <summary>
        ///     In minutes, when is the first round time this event can start
        /// </summary>
        public virtual int EarliestStart { get; } = 5;

        /// <summary>
        ///     When in the lifetime to call Start().
        /// </summary>
        protected virtual float StartAfter { get; } = 0.0f;

        /// <summary>
        ///     When in the lifetime the event should end.
        /// </summary>
        protected virtual float EndAfter { get; set; } = 0.0f;

        /// <summary>
        ///     How long has the event existed. Do not change this.
        /// </summary>
        private float Elapsed { get; set; } = 0.0f;

        /// <summary>
        ///     How many players need to be present on station for the event to run
        /// </summary>
        /// <remarks>
        ///     To avoid running deadly events with low-pop
        /// </remarks>
        public virtual int MinimumPlayers { get; } = 0;

        /// <summary>
        ///     How many times this event has run this round
        /// </summary>
        public int Occurrences { get; set; } = 0;

        /// <summary>
        ///     How many times this even can occur in a single round
        /// </summary>
        public virtual int? MaxOccurrences { get; } = null;

        /// <summary>
        ///     Has the startup time elapsed?
        /// </summary>
        protected bool Started { get; set; } = false;

        /// <summary>
        ///     Has this event commenced (announcement may or may not be used)?
        /// </summary>
        private bool Announced { get; set; } = false;

        /// <summary>
        ///     Called once to setup the event after StartAfter has elapsed.
        /// </summary>
        public virtual void Startup()
        {
            Started = true;
            Occurrences += 1;
        }

        /// <summary>
        ///     Called once as soon as an event is active.
        ///     Can also be used for some initial setup.
        /// </summary>
        public virtual void Announce()
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

            Announced = true;
            Running = true;
        }

        /// <summary>
        ///     Called once when the station event ends for any reason.
        /// </summary>
        public virtual void Shutdown()
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

            Started = false;
            Announced = false;
            Elapsed = 0;
        }

        /// <summary>
        ///     Called every tick when this event is running.
        /// </summary>
        /// <param name="frameTime"></param>
        public virtual void Update(float frameTime)
        {
            Elapsed += frameTime;

            if (!Started && Elapsed >= StartAfter)
            {
                Startup();
            }

            if (EndAfter <= Elapsed)
            {
                Running = false;
            }
        }
    }
}
