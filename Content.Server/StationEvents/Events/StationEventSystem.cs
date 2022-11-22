using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events
{
    /// <summary>
    ///     An abstract entity system inherited by all station events for their behavior.
    /// </summary>
    public abstract class StationEventSystem : GameRuleSystem
    {
        [Dependency] protected readonly IRobustRandom RobustRandom = default!;
        [Dependency] protected readonly IAdminLogManager AdminLogManager = default!;
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] protected readonly ChatSystem ChatSystem = default!;
        [Dependency] protected readonly StationSystem StationSystem = default!;

        protected ISawmill Sawmill = default!;

        /// <summary>
        ///     How long has the event existed. Do not change this.
        /// </summary>
        protected float Elapsed { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            Sawmill = Logger.GetSawmill("stationevents");
        }

        /// <summary>
        ///     Called once to setup the event after StartAfter has elapsed, or if an event is forcibly started.
        /// </summary>
        public override void Started()
        {
            AdminLogManager.Add(LogType.EventStarted, LogImpact.High, $"Event started: {Configuration.Id}");
        }

        /// <summary>
        ///     Called once as soon as an event is added, for announcements.
        ///     Can also be used for some initial setup.
        /// </summary>
        public override void Added()
        {
            AdminLogManager.Add(LogType.EventAnnounced, $"Event added / announced: {Configuration.Id}");

            if (Configuration is not StationEventRuleConfiguration ev)
                return;

            if (ev.StartAnnouncement != null)
            {
                ChatSystem.DispatchGlobalAnnouncement(Loc.GetString(ev.StartAnnouncement), playSound: false, colorOverride: Color.Gold);
            }

            if (ev.StartAudio != null)
            {
                SoundSystem.Play(ev.StartAudio.GetSound(), Filter.Broadcast(), ev.StartAudio.Params);
            }

            Elapsed = 0;
        }

        /// <summary>
        ///     Called once when the station event ends for any reason.
        /// </summary>
        public override void Ended()
        {
            AdminLogManager.Add(LogType.EventStopped, $"Event ended: {Configuration.Id}");

            if (Configuration is not StationEventRuleConfiguration ev)
                return;

            if (ev.EndAnnouncement != null)
            {
                ChatSystem.DispatchGlobalAnnouncement(Loc.GetString(ev.EndAnnouncement), playSound: false, colorOverride: Color.Gold);
            }

            if (ev.EndAudio != null)
            {
                SoundSystem.Play(ev.EndAudio.GetSound(), Filter.Broadcast(), ev.EndAudio.Params);
            }
        }

        /// <summary>
        ///     Called every tick when this event is running.
        ///     Events are responsible for their own lifetime, so this handles starting and ending after time.
        /// </summary>
        public override void Update(float frameTime)
        {
            if (!RuleAdded || Configuration is not StationEventRuleConfiguration data)
                return;

            Elapsed += frameTime;

            if (!RuleStarted && Elapsed >= data.StartAfter)
            {
                GameTicker.StartGameRule(PrototypeManager.Index<GameRulePrototype>(Prototype));
            }

            if (RuleStarted && Elapsed >= data.EndAfter)
            {
                GameTicker.EndGameRule(PrototypeManager.Index<GameRulePrototype>(Prototype));
            }
        }

        #region Helper Functions

        protected void ForceEndSelf()
        {
            GameTicker.EndGameRule(PrototypeManager.Index<GameRulePrototype>(Prototype));
        }

        protected bool TryFindRandomTile(out Vector2i tile, out EntityUid targetStation, out EntityUid targetGrid, out EntityCoordinates targetCoords)
        {
            tile = default;

            targetCoords = EntityCoordinates.Invalid;
            if (StationSystem.Stations.Count == 0)
            {
                targetStation = EntityUid.Invalid;
                targetGrid = EntityUid.Invalid;
                return false;
            }
            targetStation = RobustRandom.Pick(StationSystem.Stations);
            var possibleTargets = Comp<StationDataComponent>(targetStation).Grids;
            if (possibleTargets.Count == 0)
            {
                targetGrid = EntityUid.Invalid;
                return false;
            }

            targetGrid = RobustRandom.Pick(possibleTargets);

            if (!TryComp<MapGridComponent>(targetGrid, out var gridComp))
                return false;

            var found = false;
            var (gridPos, _, gridMatrix) = Transform(targetGrid).GetWorldPositionRotationMatrix();
            var gridBounds = gridMatrix.TransformBox(gridComp.LocalAABB);

            for (var i = 0; i < 10; i++)
            {
                var randomX = RobustRandom.Next((int) gridBounds.Left, (int) gridBounds.Right);
                var randomY = RobustRandom.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

                tile = new Vector2i(randomX - (int) gridPos.X, randomY - (int) gridPos.Y);
                if (_atmosphere.IsTileSpace(gridComp.GridEntityId, Transform(targetGrid).MapUid, tile,
                        mapGridComp: gridComp)
                    || _atmosphere.IsTileAirBlocked(gridComp.GridEntityId, tile, mapGridComp: gridComp))
                {
                    continue;
                }

                found = true;
                targetCoords = gridComp.GridTileToLocal(tile);
                break;
            }

            if (!found) return false;

            return true;
        }

        public static GameRulePrototype GetRandomEventUnweighted(IPrototypeManager? prototypeManager = null, IRobustRandom? random = null)
        {
            IoCManager.Resolve(ref prototypeManager, ref random);

            return random.Pick(prototypeManager.EnumeratePrototypes<GameRulePrototype>()
                .Where(p => p.Configuration is StationEventRuleConfiguration).ToArray());
        }

        public float GetSeverityModifier()
        {
            var ev = new GetSeverityModifierEvent();
            RaiseLocalEvent(ev);
            return ev.Modifier;
        }

        #endregion
    }

    /// <summary>
    ///     Raised broadcast to determine what the severity modifier should be for an event, some positive number that can be multiplied with various things.
    ///     Handled by usually other game rules (like the ramping scheduler).
    ///     Most events should try and make use of this if possible.
    /// </summary>
    public sealed class GetSeverityModifierEvent : EntityEventArgs
    {
        /// <summary>
        ///     Should be multiplied/added to rather than set, for commutativity.
        /// </summary>
        public float Modifier = 1.0f;
    }
}
