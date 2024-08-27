using System.Linq;
using Content.Server.Mind;
using Content.Server.Pinpointer;
using Content.Shared.MassMedia.Systems;
using Content.Shared.Mobs;
using Content.Shared.Roles;
using Content.Shared.Slippery;
using Content.Shared.Stunnable;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Replays;

public sealed class ReplayEventSystem : EntitySystem
{
    [Dependency] private readonly IReplayRecordingManager _replays = default!;
    [Dependency] private readonly ISerializationManager _serialman = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly NavMapSystem _navMapSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    private List<ReplayEvent>? _replayEvents = new();

    public override void Initialize()
    {
        _replays.RecordingStopped += ReplaysOnRecordingStopped;
        _replays.RecordingStarted += ReplaysOnRecordingStarted;

        // Using an event here because mob stuff is in shared and we cannot call RecordReplayEvent from there.
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SlipEvent>(OnSlip); // Here as well
        SubscribeLocalEvent<ActorComponent, StunnedEvent>(OnStun); // I can probably make this shared or smth
        SubscribeLocalEvent<NewsArticlePublishedEvent>(OnNewsPublished);

        base.Initialize();
    }

    private void ReplaysOnRecordingStarted(MappingDataNode arg1, List<object> arg2)
    {
        _replayEvents = new List<ReplayEvent>();
    }

    private void ReplaysOnRecordingStopped(MappingDataNode metadata)
    {
        metadata["events"] = _serialman.WriteValue(_replayEvents, true, null);
    }

        /// <summary>
    /// Records a replay event. This is the main way to record events in the replay system.
    /// </summary>
    /// <param name="replayEvent">The event to record</param>
    /// <param name="source">Optional source that will be used for location data</param>
    public void RecordReplayEvent(ReplayEvent replayEvent, EntityUid? source = null)
    {
        if (!_replays.IsRecording)
            return;

        replayEvent.Time ??= _gameTiming.CurTime.TotalSeconds;

        if (source.HasValue)
        {
            replayEvent.Position = _transformSystem.GetWorldPosition(source.Value);
            replayEvent.NearestBeacon =
                _navMapSystem.GetNearestBeaconString(_transformSystem.GetMapCoordinates(source.Value));
        }

        DebugTools.AssertNotNull(replayEvent.EventType);
        DebugTools.AssertNotNull(replayEvent.Severity);

        Log.Debug($"Recording replay event: {replayEvent.EventType}");
        if (_replayEvents == null)
        {
            // If this happens, someone messed up.
            Log.Error("Tried to record a replay event, but the events list is null. This should never happen.");
            return;
        }
        _replayEvents.Add(replayEvent);
    }

    /// <summary>
    /// Gets the player info for a player entity.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the entity is not a player.
    /// </exception>
    public ReplayEventPlayer GetPlayerInfo(EntityUid player)
    {
        if (EntityManager.TryGetComponent<ActorComponent>(player, out var actorComponent))
        {
            return GetPlayerInfo(actorComponent.PlayerSession);
        }

        Log.Warning($"Tried to get player info for entity {player}, but it's not a player entity.");
        return new ReplayEventPlayer()
        {
            PlayerGuid = new NetUserId(Guid.Empty),
            PlayerICName = EntityManager.GetComponent<MetaDataComponent>(player).EntityName, // Fallback, best we can do.
            PlayerOOCName = "Unknown",
            JobPrototypes = [],
            AntagPrototypes = [],
        };
    }

    /// <summary>
    /// Generates a <see cref="ReplayEventPlayer"/> from a session for use in replay events.
    /// </summary>
    public ReplayEventPlayer GetPlayerInfo(ICommonSession session)
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


    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        ReplayEventPlayer? targetInfo = null;
        if (EntityManager.TryGetComponent<ActorComponent>(ev.Target, out var actorComponent))
        {
            targetInfo = GetPlayerInfo(actorComponent.PlayerSession);
        }

        if (targetInfo == null)
        {
            RecordReplayEvent(new MobStateChangedNPCReplayEvent()
            {
                EventType = ReplayEventType.MobStateChanged,
                Severity = ReplayEventSeverity.Medium,
                Target = EntityManager.GetComponent<MetaDataComponent>(ev.Target).EntityName,
                OldState = ev.OldMobState,
                NewState = ev.NewMobState,
            }, ev.Target);
        }
        else
        {
            RecordReplayEvent(new MobStateChangedPlayerReplayEvent()
            {
                Target = (ReplayEventPlayer) targetInfo,
                Severity = ReplayEventSeverity.Medium,
                EventType = ReplayEventType.MobStateChanged,
                OldState = ev.OldMobState,
                NewState = ev.NewMobState,
            }, ev.Target);
        }
    }

    private void OnSlip(ref SlipEvent ev)
    {
        RecordReplayEvent(new GenericPlayerEvent()
        {
            EventType = ReplayEventType.MobSlipped,
            Severity = ReplayEventSeverity.Low,
            Target = GetPlayerInfo(ev.Slipped),
        }, ev.Slipped);
    }

    private void OnStun(EntityUid uid, ActorComponent actor, ref StunnedEvent ev)
    {
        RecordReplayEvent(new GenericPlayerEvent()
        {
            EventType = ReplayEventType.MobStunned,
            Severity = ReplayEventSeverity.Low,
            Target = GetPlayerInfo(actor.PlayerSession),
        }, uid);
    }

    private void OnNewsPublished(ref NewsArticlePublishedEvent ev)
    {
        RecordReplayEvent(new NewsArticlePublishedReplayEvent()
        {
            EventType = ReplayEventType.NewsArticlePublished,
            Severity = ReplayEventSeverity.Medium,
            Content = ev.Article.Content,
            Title = ev.Article.Title,
            Author = ev.Article.Author,
            ShareTime = ev.Article.ShareTime,
        });
    }
}
