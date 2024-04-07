using System.Linq;
using Content.Server.AlertLevel;
using Content.Server.GameTicking.Replays;
using Content.Server.Mind;
using Content.Server.Nuke;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Roles;
using Content.Shared.Slippery;
using Content.Shared.Stunnable;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private readonly IReplayRecordingManager _replays = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly ISerializationManager _serialman = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;

    private ISawmill _sawmillReplays = default!;

    private void InitializeReplays()
    {
        _replays.RecordingFinished += ReplaysOnRecordingFinished;
        _replays.RecordingStopped += ReplaysOnRecordingStopped;

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<NukeArmedEvent>(OnNukeArmed);
        SubscribeLocalEvent<NukeDisarmSuccessEvent>(OnNukeDisarmed);
        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeBoom);

        SubscribeLocalEvent<SlipEvent>(OnSlip);
        SubscribeLocalEvent<ActorComponent, StunnedEvent>(OnStun);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertChange);
    }

    /// <summary>
    /// A round has started: start recording replays if auto record is enabled.
    /// </summary>
    private void ReplayStartRound()
    {
        try
        {
            if (!_cfg.GetCVar(CCVars.ReplayAutoRecord))
                return;

            if (_replays.IsRecording)
            {
                _sawmillReplays.Warning("Already an active replay recording before the start of the round, not starting automatic recording.");
                return;
            }

            _sawmillReplays.Debug($"Starting replay recording for round {RoundId}");

            var finalPath = GetAutoReplayPath();
            var recordPath = finalPath;
            var tempDir = _cfg.GetCVar(CCVars.ReplayAutoRecordTempDir);
            ResPath? moveToPath = null;

            // Set the round end player and text back to null to prevent it from writing the previous round's data.
            _replayRoundPlayerInfo = null;
            _replayRoundText = null;
            _replayEvents = new List<ReplayEvent>();

            if (!string.IsNullOrEmpty(tempDir))
            {
                var baseReplayPath = new ResPath(_cfg.GetCVar(CVars.ReplayDirectory)).ToRootedPath();
                moveToPath = baseReplayPath / finalPath;

                var fileName = finalPath.Filename;
                recordPath = new ResPath(tempDir) / fileName;

                _sawmillReplays.Debug($"Replay will record in temporary position: {recordPath}");
            }

            var recordState = new ReplayRecordState(moveToPath);

            if (!_replays.TryStartRecording(_resourceManager.UserData, recordPath.ToString(), state: recordState))
            {
                _sawmillReplays.Error("Can't start automatic replay recording!");
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error while starting an automatic replay recording:\n{e}");
        }
    }

    public void RecordReplayEvent(ReplayEvent replayEvent)
    {
        if (!_replays.IsRecording)
            return;

        DebugTools.AssertNotNull(replayEvent.EventType);
        DebugTools.AssertNotNull(replayEvent.Severity);
        DebugTools.AssertNotNull(replayEvent.Time);

        _sawmillReplays.Debug($"Recording replay event: {replayEvent.EventType}");
        if (_replayEvents == null)
        {
            // If this happens, someone messed up.
            _sawmillReplays.Error("Tried to record a replay event, but the events list is null. This should never happen.");
            return;
        }
        _replayEvents.Add(replayEvent);
    }

    /// <summary>
    /// A round has ended: stop recording replays and make sure they're moved to the correct spot.
    /// </summary>
    private void ReplayEndRound()
    {
        try
        {
            if (_replays.ActiveRecordingState is ReplayRecordState)
            {
                _replays.StopRecording();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error while stopping replay recording:\n{e}");
        }
    }

    private void ReplaysOnRecordingFinished(ReplayRecordingFinished data)
    {
        if (data.State is not ReplayRecordState state)
            return;

        if (state.MoveToPath == null)
            return;

        _sawmillReplays.Info($"Moving replay into final position: {state.MoveToPath}");
        _taskManager.BlockWaitOnTask(_replays.WaitWriteTasks());
        DebugTools.Assert(!_replays.IsWriting());

        try
        {
            if (!data.Directory.Exists(state.MoveToPath.Value.Directory))
                data.Directory.CreateDir(state.MoveToPath.Value.Directory);
        }
        catch (UnauthorizedAccessException e)
        {
            _sawmillReplays.Error($"Error creating replay directory {state.MoveToPath.Value.Directory}: {e}");
        }

        data.Directory.Rename(data.Path, state.MoveToPath.Value);
    }

    private void ReplaysOnRecordingStopped(MappingDataNode metadata)
    {
        // Write round info like map and round end summery into the replay_final.yml file. Useful for external parsers.

        metadata["map"] = new ValueDataNode(_gameMapManager.GetSelectedMap()?.MapName);
        metadata["gamemode"] = new ValueDataNode(CurrentPreset != null ? Loc.GetString(CurrentPreset.ModeTitle) : string.Empty);
        metadata["roundEndPlayers"] = _serialman.WriteValue(_replayRoundPlayerInfo);
        metadata["roundEndText"] = new ValueDataNode(_replayRoundText);
        metadata["server_id"] = new ValueDataNode(_configurationManager.GetCVar(CCVars.ServerId));
        metadata["server_name"] = new ValueDataNode(_configurationManager.GetCVar(CCVars.AdminLogsServerName));
        metadata["roundId"] = new ValueDataNode(RoundId.ToString());
        metadata["events"] = _serialman.WriteValue(_replayEvents, true, null);
    }

    private ResPath GetAutoReplayPath()
    {
        var cfgValue = _cfg.GetCVar(CCVars.ReplayAutoRecordName);

        var time = DateTime.UtcNow;

        var interpolated = cfgValue
            .Replace("{year}", time.Year.ToString("D4"))
            .Replace("{month}", time.Month.ToString("D2"))
            .Replace("{day}", time.Day.ToString("D2"))
            .Replace("{hour}", time.Hour.ToString("D2"))
            .Replace("{minute}", time.Minute.ToString("D2"))
            .Replace("{round}", RoundId.ToString());

        return new ResPath(interpolated);
    }

    private sealed record ReplayRecordState(ResPath? MoveToPath);

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

        throw new InvalidOperationException("Tried to get player info for an entity that is not a player.");
    }

    /// <summary>
    /// Generates a <see cref="ReplayEventPlayer"/> from a session for use in replay events.
    /// </summary>
    public ReplayEventPlayer GetPlayerInfo(ICommonSession session)
    {
        var hasMind = _mindSystem.TryGetMind(session, out var mindId, out var mindComponent);

        var playerIcName = "Unknown";
        var antag = false;
        var roles = new List<RoleInfo>();
        if (hasMind && mindComponent != null)
        {
            antag = _roles.MindIsAntagonist(mindId);
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
            Antag = antag,
            JobPrototypes = roles.Where(role => !role.Antagonist).Select(role => role.Prototype).ToArray(),
            AntagPrototypes = roles.Where(role => role.Antagonist).Select(role => role.Prototype).ToArray(),
        };
    }

    #region EventListeners

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        ReplayEventPlayer? targetInfo = null;
        if (EntityManager.TryGetComponent<ActorComponent>(ev.Target, out var actorComponent))
        {
            targetInfo = GetPlayerInfo(actorComponent.PlayerSession);
        }


        if (ev.OldMobState is MobState.Dead && ev.NewMobState is MobState.Alive or MobState.Critical)
        {
            // Revived
            if (targetInfo == null)
            {
                RecordReplayEvent(new GenericObjectEvent()
                {
                    EventType = ReplayEventType.MobRevived,
                    Severity = ReplayEventSeverity.Medium,
                    Target = EntityManager.GetComponent<MetaDataComponent>(ev.Target).EntityName,
                    Time = _gameTiming.CurTick.Value
                });
            }
            else
            {
                RecordReplayEvent(new GenericPlayerEvent()
                {
                    Target = (ReplayEventPlayer) targetInfo,
                    Severity = ReplayEventSeverity.Medium,
                    EventType = ReplayEventType.MobRevived,
                    Time = _gameTiming.CurTick.Value
                });
            }
            return;
        }

        if (ev.NewMobState == MobState.Critical)
        {
            if (targetInfo == null)
            {
                RecordReplayEvent(new GenericObjectEvent()
                {
                    EventType = ReplayEventType.MobCrit,
                    Severity = ReplayEventSeverity.Medium,
                    Target = EntityManager.GetComponent<MetaDataComponent>(ev.Target).EntityName,
                    Time = _gameTiming.CurTick.Value
                });
            }
            else
            {
                RecordReplayEvent(new GenericPlayerEvent()
                {
                    Target = (ReplayEventPlayer) targetInfo,
                    Severity = ReplayEventSeverity.Medium,
                    EventType = ReplayEventType.MobCrit,
                    Time = _gameTiming.CurTick.Value
                });
            }
        }

        if (ev.NewMobState == MobState.Dead)
        {
            if (targetInfo == null)
            {
                RecordReplayEvent(new GenericObjectEvent()
                {
                    EventType = ReplayEventType.MobDied,
                    Severity = ReplayEventSeverity.Medium,
                    Target = EntityManager.GetComponent<MetaDataComponent>(ev.Target).EntityName,
                    Time = _gameTiming.CurTick.Value
                });
            }
            else
            {
                RecordReplayEvent(new GenericPlayerEvent()
                {
                    Target = (ReplayEventPlayer) targetInfo,
                    Severity = ReplayEventSeverity.Medium,
                    EventType = ReplayEventType.MobDied,
                    Time = _gameTiming.CurTick.Value
                });
            }
        }
    }

    private void OnNukeArmed(NukeArmedEvent ev)
    {
        RecordReplayEvent(new ReplayEvent()
        {
            EventType = ReplayEventType.NukeArmed,
            Severity = ReplayEventSeverity.VeryHigh,
            Time = _gameTiming.CurTick.Value
        });
    }

    private void OnNukeDisarmed(NukeDisarmSuccessEvent ev)
    {
        RecordReplayEvent(new ReplayEvent()
        {
            EventType = ReplayEventType.NukeDefused,
            Severity = ReplayEventSeverity.VeryHigh,
            Time = _gameTiming.CurTick.Value
        });
    }

    private void OnNukeBoom(NukeExplodedEvent ev)
    {

        RecordReplayEvent(new ReplayEvent()
        {
            EventType = ReplayEventType.NukeDetonated,
            Severity = ReplayEventSeverity.VeryHigh,
            Time = _gameTiming.CurTick.Value
        });
    }

    private void OnSlip(ref SlipEvent ev)
    {
        RecordReplayEvent(new GenericPlayerEvent()
        {
            EventType = ReplayEventType.MobSlipped,
            Severity = ReplayEventSeverity.Low,
            Time = _gameTiming.CurTick.Value,
            Target = GetPlayerInfo(ev.Slipped)
        });
    }

    private void OnStun(EntityUid uid, ActorComponent actor, ref StunnedEvent ev)
    {
        RecordReplayEvent(new GenericPlayerEvent()
        {
            EventType = ReplayEventType.MobStunned,
            Severity = ReplayEventSeverity.Low,
            Time = _gameTiming.CurTick.Value,
            Target = GetPlayerInfo(actor.PlayerSession)
        });
    }

    private void OnAlertChange(AlertLevelChangedEvent ev)
    {
        RecordReplayEvent(new AlertLevelChangedReplayEvent()
        {
            EventType = ReplayEventType.AlertLevelChanged,
            Severity = ReplayEventSeverity.Medium,
            Time = _gameTiming.CurTick.Value,
            AlertLevel = ev.AlertLevel
        });
    }

    #endregion
}
