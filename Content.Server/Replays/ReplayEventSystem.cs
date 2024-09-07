using System.Diagnostics;
using System.Linq;
using System.Text;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Pinpointer;
using Content.Server.Prayer;
using Content.Shared.CCVar;
using Content.Shared.MassMedia.Systems;
using Content.Shared.Mobs;
using Content.Shared.Replays;
using Content.Shared.Roles;
using Content.Shared.Slippery;
using Content.Shared.Stunnable;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Replays;

public sealed class ReplayEventSystem : SharedReplayEventSystem
{
    [Dependency] private readonly IReplayRecordingManager _replays = default!;
    [Dependency] private readonly ISerializationManager _serialman = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly NavMapSystem _navMapSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;

    private List<ReplayEvent> _replayEvents = new();
    private bool _recordEvents;

    public override void Initialize()
    {
        _replays.RecordingStopped2 += ReplaysOnRecordingStopped;
        _replays.RecordingStarted += ReplaysOnRecordingStarted;

        Subs.CVar(_cfg, CCVars.ReplayRecordEvents, OnRecordEventsChanged,true);

        base.Initialize();
    }

    private void OnRecordEventsChanged(bool enabled)
    {
        _recordEvents = enabled;
    }

    private void ReplaysOnRecordingStarted(MappingDataNode arg1, List<object> arg2)
    {
        _replayEvents = new List<ReplayEvent>();
    }

    private void ReplaysOnRecordingStopped(ReplayRecordingStopped replayRecordingStopped)
    {
        var events = _serialman.WriteValue(_replayEvents, true, null, notNullableOverride:true);
        var bytes = Encoding.UTF8.GetBytes(events.ToString());
        replayRecordingStopped.Writer.WriteBytes(replayRecordingStopped.Writer.BaseReplayPath / "events.yml", bytes);
    }

    /// <summary>
    /// Records a replay event. This is the main way to record events in the replay system.
    /// </summary>
    /// <param name="replayEvent">The event to record</param>
    /// <param name="source">Optional source that will be used for location data</param>
    public override void RecordReplayEvent(ReplayEvent replayEvent, EntityUid? source = null)
    {
        if (!_replays.IsRecording || !_recordEvents)
            return;

        replayEvent.Time ??= _gameTicker.RoundDuration().TotalSeconds;

        if (source.HasValue && replayEvent.Location == null)
        {
            replayEvent.Location = new LocationInformation
            {
                Position = _transformSystem.GetWorldPosition(source.Value),
                NearestBeacon = _navMapSystem.GetNearestBeaconString(_transformSystem.GetMapCoordinates(source.Value)),
            };

            var map = _transformSystem.GetMap(source.Value);
            if (map.HasValue)
            {
                replayEvent.Location.Map = EntityManager.GetComponent<MetaDataComponent>(map.Value).EntityName;
            }
        }

        if (replayEvent.EventType == null || replayEvent.Severity == null)
            throw new ArgumentException("Replay event must have a type and severity.");

        Log.Verbose($"Recording replay event: {replayEvent.EventType}");
        _replayEvents.Add(replayEvent);
    }

    /// <summary>
    /// Gets the player info for a player entity.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the entity is not a player.
    /// </exception>
    public override ReplayEventPlayer GetPlayerInfo(EntityUid player)
    {
        if (EntityManager.TryGetComponent<ActorComponent>(player, out var actorComponent))
        {
            return GetPlayerInfo(actorComponent.PlayerSession);
        }

        var stackTrace = new StackTrace();
        Log.Warning($"Tried to get player info for entity {player}, but it's not a player entity. Stacktrace: {stackTrace}");
        return new ReplayEventPlayer()
        {
            PlayerGuid = new NetUserId(Guid.Empty),
            PlayerICName = EntityManager.GetComponent<MetaDataComponent>(player).EntityName, // Fallback, best we can do.
            PlayerOOCName = "",
            JobPrototypes = [],
            AntagPrototypes = [],
        };
    }

    /// <summary>
    /// Generates a <see cref="ReplayEventPlayer"/> from a session for use in replay events.
    /// </summary>
    public override ReplayEventPlayer GetPlayerInfo(ICommonSession session)
    {
        var hasMind = _mindSystem.TryGetMind(session, out var mindId, out var mindComponent);

        var playerIcName = "Unknown";
        var roles = new List<RoleInfo>();
        if (hasMind && mindComponent != null)
        {
            if (mindComponent.CharacterName != null)
            {
                playerIcName = mindComponent.CharacterName;
            }
            else if (mindComponent.CurrentEntity != null && TryName(mindComponent.CurrentEntity.Value, out var name))
            {
                playerIcName = name;
            }

            roles = _roles.MindGetAllRoles(mindId);
        }

        return new ReplayEventPlayer()
        {
            PlayerGuid = session.UserId,
            PlayerICName = playerIcName,
            PlayerOOCName = session.Name,
            JobPrototypes = roles.Where(role => !role.Antagonist).Select(role => role.Prototype).ToArray(),
            AntagPrototypes = roles.Where(role => role.Antagonist).Select(role => role.Prototype).ToArray(),
        };
    }
}
