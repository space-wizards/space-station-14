using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.StationEvents.Events
{
    /// <summary>
    ///     An abstract entity system inherited by all station events for their behavior.
    /// </summary>
    public abstract class StationEventSystem : GameRuleSystem
    {
        [Dependency] protected readonly IAdminLogManager AdminLogManager = default!;
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] protected readonly ChatSystem ChatSystem = default!;
        [Dependency] protected readonly StationEventManagementSystem StationEventManager = default!;

        /// <summary>
        ///     How long has the event existed. Do not change this.
        /// </summary>
        private float Elapsed { get; set; } = 0.0f;

        /// <summary>
        ///     Has this event commenced (announcement may or may not be used)?
        /// </summary>
        private bool Announced { get; set; } = false;

        private StationEventPrototype Data

        /// <summary>
        ///     Called once to setup the event after StartAfter has elapsed, or if an event is forcibly started.
        /// </summary>
        public override void Started(GameRuleConfiguration configuration)
        {
            if (!PrototypeManager.TryIndex<StationEventPrototype>(Prototype, out var proto))
                return;

            IoCManager.Resolve<IAdminLogManager>()
                .Add(LogType.EventStarted, LogImpact.High, $"Event startup: {Name}");
        }

        /// <summary>
        ///     Called once as soon as an event is active.
        ///     Can also be used for some initial setup.
        /// </summary>
        public virtual void Announce()
        {
            IoCManager.Resolve<IAdminLogManager>()
                .Add(LogType.EventAnnounced, $"Event announce: {Name}");

            if (AnnounceEvent && StartAnnouncement != null)
            {
                var chatSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
                chatSystem.DispatchGlobalStationAnnouncement(StartAnnouncement, playDefaultSound: false, colorOverride: Color.Gold);
            }

            if (AnnounceEvent && StartAudio != null)
            {
                SoundSystem.Play(StartAudio.GetSound(), Filter.Broadcast(), AudioParams);
            }

            Announced = true;
        }

        /// <summary>
        ///     Called once when the station event ends for any reason.
        /// </summary>
        public override void Ended(GameRuleConfiguration configuration)
        {
            IoCManager.Resolve<IAdminLogManager>()
                .Add(LogType.EventStopped, $"Event shutdown: {Name}");

            if (AnnounceEvent && EndAnnouncement != null)
            {
                var chatSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
                chatSystem.DispatchGlobalStationAnnouncement(EndAnnouncement, playDefaultSound: false, colorOverride: Color.Gold);
            }

            if (AnnounceEvent && EndAudio != null)
            {
                SoundSystem.Play(EndAudio.GetSound(), Filter.Broadcast(), AudioParams);
            }

            Announced = false;
            Elapsed = 0;
        }

        /// <summary>
        ///     Called every tick when this event is running.
        /// </summary>
        /// <param name="frameTime"></param>
        public override void Update(float frameTime)
        {
            Elapsed += frameTime;

            if (!PrototypeManager.)

            if (!Started && Elapsed >= StartAfter)
            {
                StationEventManager.StartStationEvent(Prototype);
            }

            if (EndAfter <= Elapsed)
            {
                StationEventManager.EndStationEvent(Prototype);
            }
        }

        public static bool TryFindRandomTile(out Vector2i tile, out EntityUid targetStation, out EntityUid targetGrid, out EntityCoordinates targetCoords, IRobustRandom? robustRandom = null, IEntityManager? entityManager = null, IMapManager? mapManager = null, StationSystem? stationSystem = null)
        {
            tile = default;
            IoCManager.Resolve(ref robustRandom, ref entityManager, ref mapManager);
            entityManager.EntitySysManager.Resolve(ref stationSystem);

            targetCoords = EntityCoordinates.Invalid;
            if (stationSystem.Stations.Count == 0)
            {
                targetStation = EntityUid.Invalid;
                targetGrid = EntityUid.Invalid;
                return false;
            }
            targetStation = robustRandom.Pick(stationSystem.Stations);
            var possibleTargets = entityManager.GetComponent<StationDataComponent>(targetStation).Grids;
            if (possibleTargets.Count == 0)
            {
                targetGrid = EntityUid.Invalid;
                return false;
            }

            targetGrid = robustRandom.Pick(possibleTargets);

            if (!entityManager.TryGetComponent<IMapGridComponent>(targetGrid, out var gridComp))
                return false;
            var grid = gridComp.Grid;

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();
            var found = false;
            var gridBounds = grid.WorldAABB;
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
