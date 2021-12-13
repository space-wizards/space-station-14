using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.Station;
using Content.Shared.Database;
using Content.Shared.Station;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events
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

            EntitySystem.Get<AdminLogSystem>()
                .Add(LogType.EventStarted, LogImpact.High, $"Event startup: {Name}");
        }

        /// <summary>
        ///     Called once as soon as an event is active.
        ///     Can also be used for some initial setup.
        /// </summary>
        public virtual void Announce()
        {
            EntitySystem.Get<AdminLogSystem>()
                .Add(LogType.EventAnnounced, $"Event announce: {Name}");

            if (StartAnnouncement != null)
            {
                var chatManager = IoCManager.Resolve<IChatManager>();
                chatManager.DispatchStationAnnouncement(StartAnnouncement, playDefaultSound: false);
            }

            if (StartAudio != null)
            {
                SoundSystem.Play(Filter.Broadcast(), StartAudio, AudioParams.Default.WithVolume(-10f));
            }

            Announced = true;
            Running = true;
        }

        /// <summary>
        ///     Called once when the station event ends for any reason.
        /// </summary>
        public virtual void Shutdown()
        {
            EntitySystem.Get<AdminLogSystem>()
                .Add(LogType.EventStopped, $"Event shutdown: {Name}");

            if (EndAnnouncement != null)
            {
                var chatManager = IoCManager.Resolve<IChatManager>();
                chatManager.DispatchStationAnnouncement(EndAnnouncement, playDefaultSound: false);
            }

            if (EndAudio != null)
            {
                SoundSystem.Play(Filter.Broadcast(), EndAudio, AudioParams.Default.WithVolume(-10f));
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


        public static bool TryFindRandomTile(out Vector2i tile, out StationId targetStation, out EntityUid targetGrid, out EntityCoordinates targetCoords, IRobustRandom? robustRandom = null, IEntityManager? entityManager = null)
        {
            tile = default;
            robustRandom ??= IoCManager.Resolve<IRobustRandom>();
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            targetCoords = EntityCoordinates.Invalid;
            targetStation = robustRandom.Pick(entityManager.EntityQuery<StationComponent>().ToArray()).Station;
            var t = targetStation; // thanks C#
            var possibleTargets = entityManager.EntityQuery<StationComponent>()
                .Where(x => x.Station == t).ToArray();
            targetGrid = robustRandom.Pick(possibleTargets).Owner;

            if (!entityManager.TryGetComponent<IMapGridComponent>(targetGrid!, out var gridComp))
                return false;
            var grid = gridComp.Grid;

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();
            var found = false;
            var gridBounds = grid.WorldBounds;
            var gridPos = grid.WorldPosition;

            for (var i = 0; i < 10; i++)
            {
                var randomX = robustRandom.Next((int) gridBounds.Left, (int) gridBounds.Right);
                var randomY = robustRandom.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

                tile = new Vector2i(randomX - (int) gridPos.X, randomY - (int) gridPos.Y);
                if (atmosphereSystem.IsTileSpace(grid, tile) || atmosphereSystem.IsTileAirBlocked(grid, tile)) continue;
                found = true;
                targetCoords = grid.GridTileToLocal(tile);
                break;
            }

            if (!found) return false;

            return true;
        }
    }
}
