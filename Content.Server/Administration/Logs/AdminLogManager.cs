using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Systems;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Players.PlayTimeTracking;
using Prometheus;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Logs;

public sealed partial class AdminLogManager : SharedAdminLogManager, IAdminLogManager
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly IDependencyCollection _dependencies = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playtime = default!;
    [Dependency] private readonly ISharedChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IAdminLogEventPublisher _publisher = default!;
    [Dependency] private readonly ServerDbEntryManager _serverDbEntry = default!;

    public const string SawmillId = "admin.logs";

    private static readonly Histogram DatabaseUpdateTime = Metrics.CreateHistogram(
        "admin_logs_database_time",
        "Time used to send logs to the database in ms",
        new HistogramConfiguration
        {
            Buckets = Histogram.LinearBuckets(0, 0.5, 20)
        });

    private static readonly Gauge Queue = Metrics.CreateGauge(
        "admin_logs_queue",
        "How many logs are in the queue.");

    private static readonly Gauge PreRoundQueue = Metrics.CreateGauge(
        "admin_logs_pre_round_queue",
        "How many logs are in the pre-round queue.");

    private static readonly Gauge QueueCapReached = Metrics.CreateGauge(
        "admin_logs_queue_cap_reached",
        "Number of times the log queue cap has been reached in a round.");

    private static readonly Gauge PreRoundQueueCapReached = Metrics.CreateGauge(
        "admin_logs_pre_round_queue_cap_reached",
        "Number of times the pre-round log queue cap has been reached in a round.");

    private static readonly Gauge LogsSent = Metrics.CreateGauge(
        "admin_logs_sent",
        "Amount of logs sent to the database in a round.");

    // Init only
    private ISawmill _sawmill = default!;

    // CVars
    private bool _metricsEnabled;
    private TimeSpan _queueSendDelay;
    private int _queueMax;
    private int _preRoundQueueMax;
    private int _dropThreshold;
    private int _highImpactLogPlaytime;

    // Per update
    private TimeSpan _nextUpdateTime;
    private readonly ConcurrentQueue<AdminLogEventWriteData> _logQueue = new();
    private readonly ConcurrentQueue<AdminLogEventWriteData> _preRoundLogQueue = new();

    // Condensation carry-over: groups that didn't meet the threshold yet carry across flushes.
    // Keyed by (ServerId, RoundId, LogType, Player). Flushed on shutdown and round-end.
    private readonly Dictionary<(int ServerId, int RoundId, LogType Type, Guid Player), List<AdminLogEventWriteData>>
        _carryOverBuckets = new();

    // Condensation CVars
    private bool _condensationEnabled;
    private TimeSpan _condensationMaxGap;

    // Per round
    private int _currentRoundId;
    private GameRunLevel _runLevel = GameRunLevel.PreRoundLobby;

    // 1 when saving, 0 otherwise
    private int _savingLogs;
    private int _logsDropped;

    private int _serverId;
    private string _serverName = "unknown";

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);

        InitializeJson();

        _configuration.OnValueChanged(CVars.MetricsEnabled,
            value => _metricsEnabled = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsEnabled,
            value => Enabled = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsQueueSendDelay,
            value => _queueSendDelay = TimeSpan.FromSeconds(value), true);
        _configuration.OnValueChanged(CCVars.AdminLogsQueueMax,
            value => _queueMax = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsPreRoundQueueMax,
            value => _preRoundQueueMax = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsDropThreshold,
            value => _dropThreshold = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsHighLogPlaytime,
            value => _highImpactLogPlaytime = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsCondensationEnabled,
            value => _condensationEnabled = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsCondensationMaxGap,
            value => _condensationMaxGap = TimeSpan.FromSeconds(value), true);

        _ = Task.Run(async () =>
        {
            //this fixes a race
            try
            {
                await EnsureServerIdentity();
            }
            catch (Exception e)
            {
                _sawmill.Warning($"Failed to resolve admin-log server identity during initialization: {e}");
            }
        });

        if (_metricsEnabled)
        {
            PreRoundQueueCapReached.Set(0);
            QueueCapReached.Set(0);
            LogsSent.Set(0);
        }
    }

    public override string ConvertName(string name)
    {
        // JsonNamingPolicy is not whitelisted by the sandbox.
        return NamingPolicy.ConvertName(name);
    }

    public async Task Shutdown()
    {
        // Flush any carry-over condensation buckets before final save.
        var carryOver = FlushCarryOver();
        foreach (var log in carryOver)
            _logQueue.Enqueue(log);

        if (!_logQueue.IsEmpty || !_preRoundLogQueue.IsEmpty)
        {
            await SaveLogs(dropPreRoundInLobby: false);
        }
    }

    public async void Update()
    {
        if (_runLevel == GameRunLevel.PreRoundLobby)
        {
            await PreRoundUpdate();
            return;
        }

        var count = _logQueue.Count;
        Queue.Set(count);

        var preRoundCount = _preRoundLogQueue.Count;
        PreRoundQueue.Set(preRoundCount);

        if (count + preRoundCount == 0)
        {
            return;
        }

        if (_timing.RealTime >= _nextUpdateTime)
        {
            await TrySaveLogs();
            return;
        }

        if (count >= _queueMax)
        {
            if (_metricsEnabled)
            {
                QueueCapReached.Inc();
            }

            await TrySaveLogs();
        }
    }

    private async Task PreRoundUpdate()
    {
        var preRoundCount = _preRoundLogQueue.Count;
        PreRoundQueue.Set(preRoundCount);

        if (preRoundCount < _preRoundQueueMax)
        {
            return;
        }

        if (_metricsEnabled)
        {
            PreRoundQueueCapReached.Inc();
        }

        await TrySaveLogs();
    }

    private async Task TrySaveLogs()
    {
        if (Interlocked.Exchange(ref _savingLogs, 1) == 1)
            return;

        try
        {
            await SaveLogs();
        }
        finally
        {
            Interlocked.Exchange(ref _savingLogs, 0);
        }
    }

    private async Task SaveLogs(bool dropPreRoundInLobby = true)
    {
        _nextUpdateTime = _timing.RealTime.Add(_queueSendDelay);

        var inRoundCount = _logQueue.Count;
        var preRoundCount = _preRoundLogQueue.Count;

        // TODO ADMIN LOGS array pool
        var copy = new List<AdminLogEventWriteData>(inRoundCount + preRoundCount);

        while (_logQueue.TryDequeue(out var inRoundLog))
        {
            copy.Add(inRoundLog);
        }

        if (inRoundCount >= _queueMax)
        {
            _sawmill.Warning($"In-round cap of {_queueMax} reached for admin logs.");
        }

        var dropped = Interlocked.Exchange(ref _logsDropped, 0);
        if (dropped > 0)
        {
            _sawmill.Error($"Dropped {dropped} logs. Current max threshold: {_dropThreshold}");
        }

        if (dropPreRoundInLobby && _runLevel == GameRunLevel.PreRoundLobby && preRoundCount > 0)
        {
            var droppedPreRound = 0;
            while (_preRoundLogQueue.TryDequeue(out _))
            {
                droppedPreRound++;
            }

            _sawmill.Error($"Dropping {droppedPreRound} pre-round logs. Current cap: {_preRoundQueueMax}");
        }
        else
        {
            while (_preRoundLogQueue.TryDequeue(out var preRoundLog))
            {
                CacheLog(preRoundLog);
                copy.Add(preRoundLog);
            }
        }

        Queue.Set(0);
        PreRoundQueue.Set(0);

        try
        {
            await EnsureServerIdentity();

            for (var i = 0; i < copy.Count; i++)
            {
                var log = copy[i];

                if (log.ServerId <= 0)
                    log.ServerId = _serverId;

                if (string.IsNullOrWhiteSpace(log.ServerName))
                    log.ServerName = _serverName;
            }

            // Round ID is unknown for pre-round logs. Attach them to the active round before persistence.
            for (var i = copy.Count - 1; i >= 0; i--)
            {
                var log = copy[i];
                if (log.RoundId > 0)
                    continue;

                if (_currentRoundId > 0)
                {
                    copy[i] = new AdminLogEventWriteData
                    {
                        ServerId = log.ServerId,
                        ServerName = log.ServerName,
                        RoundId = _currentRoundId,
                        Type = log.Type,
                        Impact = log.Impact,
                        OccurredAt = log.OccurredAt,
                        Message = log.Message,
                        Json = log.Json,
                        Players = log.Players,
                        Entities = log.Entities,
                        PlayerRoles = log.PlayerRoles,
                    };
                    continue;
                }

                _sawmill.Warning($"Dropping admin log with unresolved round id. Type: {log.Type}, Message: {log.Message}");
                copy.RemoveAt(i);
            }

            copy = CondenseLogs(copy);

            if (copy.Count == 0)
                return;

            _sawmill.Debug($"Saving {copy.Count} admin logs.");

            if (_metricsEnabled)
            {
                LogsSent.Inc(copy.Count);

                using (DatabaseUpdateTime.NewTimer())
                {
                    await _db.AddAdminLogs(copy);
                }
            }
            else
            {
                await _db.AddAdminLogs(copy);
            }

            PublishStructuredLogs(copy);
        }
        catch (Exception e)
        {
            var targetQueue = _runLevel == GameRunLevel.PreRoundLobby ? _preRoundLogQueue : _logQueue;

            foreach (var log in copy)
            {
                if (targetQueue.Count >= _dropThreshold)
                {
                    Interlocked.Increment(ref _logsDropped);
                    continue;
                }

                targetQueue.Enqueue(log);
            }

            _sawmill.Error($"Failed to persist admin logs. Re-queued {copy.Count} logs. Structured publish skipped. Error: {e}");
        }
    }

    private void PublishStructuredLogs(List<AdminLogEventWriteData> logs)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var log in logs)
                {
                    var logEvent = new StructuredAdminLogEvent(
                        log.ServerId,
                        log.ServerName,
                        log.RoundId,
                        log.LogId,
                        log.Type,
                        log.Impact,
                        log.OccurredAt,
                        log.Message,
                        log.Json,
                        log.Players.ToArray(),
                        log.Entities.Select(e => new AdminLogEntityPayload(e.EntityUid, e.Role, e.PrototypeId, e.EntityName)).ToArray());

                    await _publisher.PublishAsync(logEvent);
                }
            }
            catch (Exception e)
            {
                _sawmill.Warning($"Failed publishing admin logs: {e}");
            }
        });
    }

    public void RoundStarting(int id)
    {
        _currentRoundId = id;
        CacheNewRound();
    }

    public void RunLevelChanged(GameRunLevel level)
    {
        _runLevel = level;

        if (level == GameRunLevel.PreRoundLobby)
        {
            // Flush any remaining carry-over condensation buckets on round end.
            var carryOver = FlushCarryOver();
            foreach (var log in carryOver)
                _logQueue.Enqueue(log);

            if (!_preRoundLogQueue.IsEmpty)
            {
                // This technically means that you could get pre-round logs from
                // a previous round passed onto the next one
                // If this happens please file a complaint with your nearest lottery
                // V2 logs use database identity keys
            }

            if (_metricsEnabled)
            {
                PreRoundQueueCapReached.Set(0);
                QueueCapReached.Set(0);
                LogsSent.Set(0);
            }
        }
    }

    public override void AddStructured(
        LogType type,
        LogImpact impact,
        ref LogStringHandler handler,
        object? payload = null,
        IReadOnlyCollection<Guid>? players = null,
        IReadOnlyCollection<AdminLogEntityRef>? entities = null,
        IReadOnlyDictionary<Guid, AdminLogEntityRole>? playerRoles = null)
    {
        var message = handler.ToStringAndClear();
        if (!Enabled)
            return;

        var json = payload is JsonDocument doc
            ? doc
            : payload != null
                ? JsonSerializer.SerializeToDocument(payload)
                : JsonSerializer.SerializeToDocument(new { });

        var preRound = _runLevel == GameRunLevel.PreRoundLobby;
        var count = preRound ? _preRoundLogQueue.Count : _logQueue.Count;
        if (count >= _dropThreshold)
        {
            Interlocked.Increment(ref _logsDropped);
            return;
        }

        var handlerValues = handler.Values;
        var autoPlayers = GetPlayers(handlerValues);
        var autoEntities = GetEntities(handlerValues, type);
        var autoPlayerRoles = GetPlayerRoles(handlerValues, type);
        var logPlayers = new List<Guid>(autoPlayers.Count + (players?.Count ?? 0));

        if (players != null)
        {
            foreach (var player in players)
            {
                AddPlayer(logPlayers, player);
            }
        }

        foreach (var player in autoPlayers)
        {
            AddPlayer(logPlayers, player);
        }

        var logEntities = new List<AdminLogEventEntityWriteData>(autoEntities.Count + (entities?.Count ?? 0));
        if (entities != null)
        {
            foreach (var entity in entities)
            {
                var prototypeId = entity.PrototypeId;
                var entityName = entity.EntityName;

                if ((prototypeId == null || entityName == null)
                    && EntityManager.TryGetComponent<MetaDataComponent>(entity.Entity, out var meta))
                {
                    prototypeId ??= meta.EntityPrototype?.ID;
                    entityName ??= meta.EntityName;
                }

                if (entityName == null && preRound)
                    entityName = "[PreRound]";

                AddEntity(logEntities, (int) entity.Entity, entity.Role, prototypeId, entityName);
            }
        }

        foreach (var autoEntity in autoEntities)
        {
            AddEntity(logEntities, autoEntity.EntityUid, autoEntity.Role, autoEntity.PrototypeId, autoEntity.EntityName);
        }

        Dictionary<Guid, AdminLogEntityRole>? mergedPlayerRoles = null;
        if (autoPlayerRoles != null || playerRoles != null)
        {
            mergedPlayerRoles = new Dictionary<Guid, AdminLogEntityRole>();

            if (autoPlayerRoles != null)
            {
                foreach (var (guid, role) in autoPlayerRoles)
                    mergedPlayerRoles.TryAdd(guid, role);
            }

            if (playerRoles != null)
            {
                foreach (var (guid, role) in playerRoles)
                    mergedPlayerRoles[guid] = role;
            }
        }

        if (message.Contains('\0'))
        {
            _sawmill.Error($"Null character detected in admin log message '{message}'! LogType: {type}, LogImpact: {impact}");
            message = message.Replace("\0", "");
        }

        var log = new AdminLogEventWriteData
        {
            ServerId = _serverId,
            ServerName = _serverName,
            RoundId = _currentRoundId,
            Type = type,
            Impact = impact,
            OccurredAt = DateTime.UtcNow,
            Message = message,
            Json = json,
            Players = logPlayers,
            Entities = logEntities,
            PlayerRoles = mergedPlayerRoles?.Count > 0 ? mergedPlayerRoles : null,
        };

        if (preRound)
        {
            _preRoundLogQueue.Enqueue(log);
        }
        else
        {
            _logQueue.Enqueue(log);
            CacheLog(log);
        }
    }

    private List<Guid> GetPlayers(Dictionary<string, object?> values)
    {
        List<Guid> players = new();
        foreach (var value in values.Values)
        {
            switch (value)
            {
                case SerializablePlayer player:
                    AddPlayer(players, player.UserId);
                    continue;

                case EntityStringRepresentation rep:
                    if (rep.Session is {} session)
                        AddPlayer(players, session.UserId.UserId);
                    continue;

                case IAdminLogsPlayerValue playerValue:
                    foreach (var player in playerValue.Players)
                    {
                        AddPlayer(players, player);
                    }

                    break;
            }
        }

        return players;
    }

    /// <summary>
    /// Builds a per-player role map from the log values.
    /// When a log value carries a player session,
    /// the role inferred from the field key is recorded for that player GUID.
    /// This lets <c>AddAdminLogs</c> write meaningful roles on player participant rows instead of
    /// always defaulting to whatever the actor is.
    /// Returns <c>null</c> when no player-entity associations are found
    /// </summary>
    private Dictionary<Guid, AdminLogEntityRole>? GetPlayerRoles(Dictionary<string, object?> values, LogType type)
    {
        Dictionary<Guid, AdminLogEntityRole>? roles = null;

        foreach (var (key, value) in values)
        {
            if (value is not EntityStringRepresentation rep)
                continue;
            if (rep.Session is not { } session)
                continue;

            var role = GetEntityRole(type, key);
            roles ??= new Dictionary<Guid, AdminLogEntityRole>();
            // First role wins if the same player appears under multiple keys.
            roles.TryAdd(session.UserId.UserId, role);
        }

        return roles;
    }

     private List<AdminLogEventEntityWriteData> GetEntities(Dictionary<string, object?> values, LogType type)
    {
        var preRound = _runLevel == GameRunLevel.PreRoundLobby;
        var entities = new List<AdminLogEventEntityWriteData>();

        foreach (var (key, value) in values)
        {
            var role = GetEntityRole(type, key);

            if (value is EntityStringRepresentation rep)
            {
                var name = rep.Name;
                if (name == null && preRound)
                    name = "[PreRound]";

                AddEntity(entities, (int) rep.Uid, role, rep.Prototype, name);
            }
        }

        return entities;
    }

    private static AdminLogEntityRole GetEntityRole(LogType type, string key)
    {
        key = key.ToLowerInvariant();

        // Prefer explicit log-type semantics for events,
        // then fall back to generic key-based role inference for all other log types.
        switch (type)
        {
            //Combat/damage stuff
            case LogType.Damaged:
            case LogType.Healed:
            case LogType.MeleeHit:
            case LogType.BulletHit:
            case LogType.HitScanHit:
            case LogType.Electrocution:
            case LogType.ThrowHit:
                if (ContainsAny(key, "attacker", "source", "shooter", "thrower", "actor", "user", "player"))
                    return AdminLogEntityRole.Actor;
                if (ContainsAny(key, "victim", "target"))
                    return AdminLogEntityRole.Victim;
                if (ContainsAny(key, "weapon", "tool", "instrument", "projectile", "thrown"))
                    return AdminLogEntityRole.Tool;
                break;

            //Item movement
            case LogType.Pickup:
            case LogType.Drop:
            case LogType.Throw:
            case LogType.Landed:
                if (ContainsAny(key, "actor", "user", "player", "thrower"))
                    return AdminLogEntityRole.Actor;
                if (ContainsAny(key, "item", "thrown", "target", "entity"))
                    return AdminLogEntityRole.Target;
                if (ContainsAny(key, "container", "slot"))
                    return AdminLogEntityRole.Container;
                break;

            //tool use
            case LogType.InteractUsing:
                if (ContainsAny(key, "user", "actor", "player"))
                    return AdminLogEntityRole.Actor;
                if (ContainsAny(key, "used", "tool", "weapon", "instrument"))
                    return AdminLogEntityRole.Tool;
                if (ContainsAny(key, "target", "entity"))
                    return AdminLogEntityRole.Target;
                break;

            //identity
            case LogType.Identity:
                if (ContainsAny(key, "name", "actor", "player", "user", "entity"))
                    return AdminLogEntityRole.Actor;
                break;
        }

        // generic fallbacks
        if (ContainsAny(key, "actor", "user", "player", "attacker"))
            return AdminLogEntityRole.Actor;
        if (ContainsAny(key, "target", "recipient", "entity"))
            return AdminLogEntityRole.Target;
        if (ContainsAny(key, "tool", "weapon", "instrument", "projectile", "using"))
            return AdminLogEntityRole.Tool;
        if (key.Contains("victim"))
            return AdminLogEntityRole.Victim;
        if (ContainsAny(key, "container", "slot"))
            return AdminLogEntityRole.Container;
        if (ContainsAny(key, "location", "coord", "subject", "grid", "station", "map"))
            return AdminLogEntityRole.Subject;

        return AdminLogEntityRole.Other;
    }

    private static bool ContainsAny(string input, params string[] values)
    {
        foreach (var value in values)
        {
            if (input.Contains(value))
                return true;
        }

        return false;
    }

    private void AddEntity(
        List<AdminLogEventEntityWriteData> entities,
        int entityUid,
        AdminLogEntityRole role,
        string? prototypeId = null,
        string? entityName = null)
    {
        foreach (var entity in entities)
        {
            if (entity.EntityUid == entityUid && entity.Role == role)
                return;
        }

        entities.Add(new AdminLogEventEntityWriteData
        {
            EntityUid = entityUid,
            Role = role,
            PrototypeId = prototypeId,
            EntityName = entityName,
        });
    }

    /// <summary>
    /// Get a list of coordinates from the <see cref="LogStringHandler"/>s values. Will transform all coordinate types
    /// to map coordinates!
    /// </summary>
    /// <returns>A list of map coordinates that were found in the value input, can return an empty list.</returns>
    private List<MapCoordinates> GetCoordinates(Dictionary<string, object?> values)
    {
        List<MapCoordinates> coordList = new();
        EntityManager.TrySystem(out TransformSystem? transform);

        foreach (var value in values.Values)
        {
            switch (value)
            {
                case EntityCoordinates entCords:
                    if (transform != null)
                        coordList.Add(transform.ToMapCoordinates(entCords));
                    continue;

                case MapCoordinates mapCord:
                    coordList.Add(mapCord);
                    continue;
            }
        }

        return coordList;
    }

    private void AddPlayer(List<Guid> players, Guid user)
    {
        // The majority of logs have a single player, or maybe two, not anymore :godo:. Instead of allocating a List<AdminLogPlayer> and
        // HashSet<Guid>, we just iterate over the list to check for duplicates.
        foreach (var player in players)
        {
            if (player == user)
                return;
        }

        players.Add(user);
    }

    private void DoAdminAlerts(List<Guid> players, string message, LogImpact impact, LogStringHandler handler)
    {
        var adminLog = false;
        var logMessage = message;
        var playerNetEnts = new List<(NetEntity, string)>();

        foreach (var id in players)
        {

            if (EntityManager.TrySystem(out AdminSystem? adminSys))
            {
                var cachedInfo = adminSys.GetCachedPlayerInfo(new NetUserId(id));
                if (cachedInfo != null && cachedInfo.Antag)
                {
                    var proto = cachedInfo.RoleProto == null ? null : _proto.Index(cachedInfo.RoleProto.Value);
                    var subtype = Loc.GetString(cachedInfo.Subtype ?? proto?.Name ?? RoleTypePrototype.FallbackName);
                    logMessage = Loc.GetString(
                        "admin-alert-antag-label",
                        ("message", logMessage),
                        ("name", cachedInfo.CharacterName),
                        ("subtype", subtype));
                }
                if (cachedInfo != null && cachedInfo.NetEntity != null)
                    playerNetEnts.Add((cachedInfo.NetEntity.Value, cachedInfo.CharacterName));
            }

            if (adminLog)
                continue;

            if (impact == LogImpact.Extreme) // Always chat-notify Extreme logs
                adminLog = true;

            if (impact == LogImpact.High) // Only chat-notify High logs if the player is below a threshold playtime
            {
                if (_highImpactLogPlaytime >= 0 && _player.TryGetSessionById(new NetUserId(id), out var session))
                {
                    var playtimes = _playtime.GetPlayTimes(session);
                    if (playtimes.TryGetValue(PlayTimeTrackingShared.TrackerOverall, out var overallTime) &&
                        overallTime <= TimeSpan.FromHours(_highImpactLogPlaytime))
                    {
                        adminLog = true;
                    }
                }
            }
        }

        if (adminLog)
        {
            _chat.SendAdminAlert(logMessage);

            if (CreateTpLinks(playerNetEnts, out var tpLinks))
                _chat.SendAdminAlertNoFormatOrEscape(tpLinks);

            var coords = GetCoordinates(handler.Values);

            if (CreateCordLinks(coords, out var cordLinks))
                _chat.SendAdminAlertNoFormatOrEscape(cordLinks);
        }
    }

    /// <summary>
    /// Creates a list of tpto command links of the given players
    /// </summary>
    private bool CreateTpLinks(List<(NetEntity NetEnt, string CharacterName)> players, out string outString)
    {
        outString = string.Empty;

        if (players.Count == 0)
            return false;

        outString = Loc.GetString("admin-alert-tp-to-players-header");

        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i];
            outString += $"[cmdlink=\"{FormattedMessage.EscapeStringParameter(player.CharacterName)}\" command=\"tpto {player.NetEnt}\"/]";

            if (i < players.Count - 1)
                outString += ", ";
        }

        return true;
    }

    /// <summary>
    /// Creates a list of toto command links for the given map coordinates.
    /// </summary>
    private bool CreateCordLinks(List<MapCoordinates> cords, out string outString)
    {
        outString = string.Empty;

        if (cords.Count == 0)
            return false;

        outString = Loc.GetString("admin-alert-tp-to-coords-header");

        for (var i = 0; i < cords.Count; i++)
        {
            var cord = cords[i];
            outString += $"[cmdlink=\"{cord.ToString()}\" command=\"tp {cord.X} {cord.Y} {cord.MapId}\"/]";

            if (i < cords.Count - 1)
                outString += ", ";
        }

        return true;
    }

    public async Task<List<SharedAdminLog>> All(LogFilter? filter = null, Func<List<SharedAdminLog>>? listProvider = null)
    {
        if (_serverId <= 0)
            await EnsureServerIdentity();

        filter = ApplyServerScope(filter);

        if (TrySearchCache(filter, out var results))
        {
            return results;
        }

        var initialSize = Math.Min(filter?.Limit ?? 0, 1000);
        List<SharedAdminLog> list;
        if (listProvider != null)
        {
            list = listProvider();
            list.EnsureCapacity(initialSize);
        }
        else
        {
            list = new List<SharedAdminLog>(initialSize);
        }

        await foreach (var log in _db.GetAdminLogs(filter).WithCancellation(filter?.CancellationToken ?? default))
        {
            list.Add(log);
        }

        return list;
    }

    public async IAsyncEnumerable<string> AllMessages(LogFilter? filter = null)
    {
        if (_serverId <= 0)
            await EnsureServerIdentity();

        filter = ApplyServerScope(filter);

        await foreach (var message in _db.GetAdminLogMessages(filter))
        {
            yield return message;
        }
    }

    public async IAsyncEnumerable<JsonDocument> AllJson(LogFilter? filter = null)
    {
        if (_serverId <= 0)
            await EnsureServerIdentity();

        filter = ApplyServerScope(filter);

        await foreach (var json in _db.GetAdminLogsJson(filter))
        {
            yield return json;
        }
    }

    public Task<Round> Round(int roundId)
    {
        return _db.GetRound(roundId);
    }

    public Task<List<SharedAdminLog>> CurrentRoundLogs(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _currentRoundId;
        return All(filter);
    }

    public IAsyncEnumerable<string> CurrentRoundMessages(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _currentRoundId;
        return AllMessages(filter);
    }

    public IAsyncEnumerable<JsonDocument> CurrentRoundJson(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _currentRoundId;
        return AllJson(filter);
    }

    public Task<Round> CurrentRound()
    {
        return Round(_currentRoundId);
    }

    public async Task<int> CountLogs(int round, int? serverId = null, CancellationToken cancel = default)
    {
        if (serverId == null && _serverId <= 0)
            await EnsureServerIdentity();

        var scopedServerId = serverId ?? (_serverId > 0 ? _serverId : null);
        return await _db.CountAdminLogs(round, scopedServerId, cancel);
    }

    /// <summary>
    /// Ensures the filter has a Server.Id set.
    /// defaults to the current server's resolved identity.
    /// This keeps every query index-friendly on the Server Id composite indexes
    /// without requiring every call-site to remember to set ServerId manually.
    /// </summary>
    private LogFilter ApplyServerScope(LogFilter? filter)
    {
        filter ??= new LogFilter();
        filter.ServerId ??= _serverId > 0 ? _serverId : null;
        return filter;
    }

    /// <summary>
    /// Condenses high-frequency low-signal log events into summary entries.
    /// Groups eligible events by (ServerId, RoundId, LogType, player) using a
    /// gap-based time window, then replaces groups exceeding the type's minimum
    /// group size with a single condensed event.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The condensed event preserves the original <see cref="LogType"/> so it remains
    /// searchable by type filters. Condensation metadata (count, time window, sample
    /// messages) is stored in the JSON payload.
    /// </para>
    /// <para>
    /// Groups that don't yet meet their threshold are carried over to the next flush
    /// via <see cref="_carryOverBuckets"/>, so bursts that span flush boundaries are
    /// still condensed. Call <see cref="FlushCarryOver"/> on shutdown/round-end to
    /// emit any remaining carry-over events.
    /// </para>
    /// </remarks>
    private List<AdminLogEventWriteData> CondenseLogs(List<AdminLogEventWriteData> logs)
    {
        if (!_condensationEnabled)
            return logs;

        // Tag each log with its original position so we can restore chronological order.
        var tagged = new List<(AdminLogEventWriteData Log, int Seq)>(logs.Count);
        for (var i = 0; i < logs.Count; i++)
            tagged.Add((logs[i], i));

        var result = new List<(AdminLogEventWriteData Log, int Seq)>(logs.Count);

        // Merge incoming eligible events into carry-over buckets.
        var nextSeq = logs.Count; // Sequence counter for carry-over events already in buckets.
        foreach (var (log, seq) in tagged)
        {
            if (!LogCondensationPolicy.IsEligible(log.Type, log.Impact, log.Players.Count))
            {
                result.Add((log, seq));
                continue;
            }

            var key = (log.ServerId, log.RoundId, log.Type, log.Players[0]);

            if (!_carryOverBuckets.TryGetValue(key, out var bucket))
            {
                bucket = new List<AdminLogEventWriteData>(8);
                _carryOverBuckets[key] = bucket;
            }

            bucket.Add(log);
        }

        // Process each carry-over bucket: split into time-windowed sub-groups.
        var keysToRemove = new List<(int, int, LogType, Guid)>();
        foreach (var (key, bucket) in _carryOverBuckets)
        {
            var minGroupSize = LogCondensationPolicy.GetMinGroupSize(key.Type);
            if (minGroupSize == null)
            {
                // Type lost its rule (shouldn't happen, but be safe) — emit individually.
                foreach (var log in bucket)
                    result.Add((log, nextSeq++));
                keysToRemove.Add(key);
                continue;
            }

            // Sort by time within the bucket.
            bucket.Sort((a, b) => a.OccurredAt.CompareTo(b.OccurredAt));

            // Split into completed sub-groups using gap-based windowing.
            // The last sub-group may be "open" (not yet expired) — keep it in carry-over.
            var subGroupStart = 0;
            var lastEmittedEnd = 0;

            for (var i = 1; i <= bucket.Count; i++)
            {
                bool gapExceeded;
                if (i == bucket.Count)
                {
                    // Check if the tail of the bucket is old enough to close.
                    // If the last event is older than MaxEventGap from now, the group is closed.
                    // Otherwise, keep it in carry-over for the next flush.
                    var timeSinceLast = DateTime.UtcNow - bucket[i - 1].OccurredAt;
                    gapExceeded = timeSinceLast > _condensationMaxGap;
                }
                else
                {
                    gapExceeded = (bucket[i].OccurredAt - bucket[i - 1].OccurredAt) > _condensationMaxGap;
                }

                if (!gapExceeded && i < bucket.Count)
                    continue;

                // If we're at the end and the gap hasn't expired, this sub-group stays in carry-over.
                if (i == bucket.Count && !gapExceeded)
                    break;

                var subGroupSize = i - subGroupStart;
                if (subGroupSize >= minGroupSize.Value)
                {
                    var condensed = BuildCondensedEvent(key.Type, bucket, subGroupStart, i);
                    result.Add((condensed, nextSeq++));
                }
                else
                {
                    for (var j = subGroupStart; j < i; j++)
                        result.Add((bucket[j], nextSeq++));
                }

                lastEmittedEnd = i;
                subGroupStart = i;
            }

            // Keep only the un-emitted tail in the carry-over bucket.
            if (lastEmittedEnd >= bucket.Count)
            {
                keysToRemove.Add(key);
            }
            else if (lastEmittedEnd > 0)
            {
                bucket.RemoveRange(0, lastEmittedEnd);
            }
        }

        foreach (var key in keysToRemove)
            _carryOverBuckets.Remove(key);

        // Restore chronological order: sort by OccurredAt, then by original sequence as tiebreaker.
        result.Sort((a, b) =>
        {
            var cmp = a.Log.OccurredAt.CompareTo(b.Log.OccurredAt);
            return cmp != 0 ? cmp : a.Seq.CompareTo(b.Seq);
        });

        var output = new List<AdminLogEventWriteData>(result.Count);
        foreach (var (log, _) in result)
            output.Add(log);

        return output;
    }

    /// <summary>
    /// Flushes all carry-over condensation buckets, emitting whatever is accumulated
    /// regardless of whether thresholds are met. Called on shutdown and round-end
    /// to ensure no events are silently lost.
    /// </summary>
    private List<AdminLogEventWriteData> FlushCarryOver()
    {
        var flushed = new List<AdminLogEventWriteData>();

        foreach (var (key, bucket) in _carryOverBuckets)
        {
            if (bucket.Count == 0)
                continue;

            bucket.Sort((a, b) => a.OccurredAt.CompareTo(b.OccurredAt));

            var minGroupSize = LogCondensationPolicy.GetMinGroupSize(key.Type);

            if (minGroupSize != null && bucket.Count >= minGroupSize.Value)
            {
                flushed.Add(BuildCondensedEvent(key.Type, bucket, 0, bucket.Count));
            }
            else
            {
                flushed.AddRange(bucket);
            }
        }

        _carryOverBuckets.Clear();

        // Sort by time before returning.
        flushed.Sort((a, b) => a.OccurredAt.CompareTo(b.OccurredAt));
        return flushed;
    }

    /// <summary>
    /// Builds a single condensed event from a slice of the bucket.
    /// Preserves the original <see cref="LogType"/> (no burst type rewriting).
    /// Uses the maximum <see cref="LogImpact"/> across the group to avoid down-ranking.
    /// </summary>
    private AdminLogEventWriteData BuildCondensedEvent(
        LogType type,
        List<AdminLogEventWriteData> bucket,
        int start,
        int end)
    {
        var count = end - start;
        var first = bucket[start];
        var last = bucket[end - 1];
        var windowSeconds = (last.OccurredAt - first.OccurredAt).TotalSeconds;

        // Use the maximum impact across the group to avoid hiding medium events in a low burst.
        var maxImpact = first.Impact;

        // Merge all distinct players.
        var allPlayers = new List<Guid>();
        // Merge per-player roles (first role seen for each player wins).
        Dictionary<Guid, AdminLogEntityRole>? allPlayerRoles = null;
        // Merge all distinct entities, keeping role information. Use HashSet for O(1) dedup.
        var seenEntities = new HashSet<(int EntityUid, AdminLogEntityRole Role)>();
        var allEntities = new List<AdminLogEventEntityWriteData>();
        // Collect distinct target/object entity names for the summary message.
        var entityNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        // Sample messages for the JSON payload.
        var sampleMessages = new List<string>(LogCondensationPolicy.MaxSampleMessages);

        for (var i = start; i < end; i++)
        {
            var log = bucket[i];

            if (log.Impact > maxImpact)
                maxImpact = log.Impact;

            foreach (var player in log.Players)
                AddPlayer(allPlayers, player);

            // Merge player role overrides from constituent events.
            if (log.PlayerRoles != null)
            {
                allPlayerRoles ??= new Dictionary<Guid, AdminLogEntityRole>();
                foreach (var (guid, role) in log.PlayerRoles)
                    allPlayerRoles.TryAdd(guid, role);
            }

            foreach (var entity in log.Entities)
            {
                // O(1) deduplicate entities by (uid, role).
                if (seenEntities.Add((entity.EntityUid, entity.Role)))
                    allEntities.Add(entity);

                // Track target/object entity names for the summary.
                if (entity.EntityName != null && entity.Role != AdminLogEntityRole.Actor)
                {
                    entityNameCounts.TryGetValue(entity.EntityName, out var c);
                    entityNameCounts[entity.EntityName] = c + 1;
                }
            }

            if (sampleMessages.Count < LogCondensationPolicy.MaxSampleMessages)
                sampleMessages.Add(log.Message);
        }

        var typeName = type.ToString();

        // Show at least 1s when all events occurred in the same tick.
        var displaySeconds = Math.Max(1, (int) Math.Round(windowSeconds));

        // Build human-readable summary message.
        string summary;

        if (entityNameCounts.Count > 0)
        {
            // Entity-aware summary: list the most-seen non-Actor entity names.
            var topNames = entityNameCounts
                .OrderByDescending(kv => kv.Value)
                .Take(LogCondensationPolicy.MaxEntityNamesInSummary)
                .Select(kv => kv.Value > 1 ? $"{kv.Key} ×{kv.Value}" : kv.Key);

            var suffix = entityNameCounts.Count > LogCondensationPolicy.MaxEntityNamesInSummary
                ? $", +{entityNameCounts.Count - LogCondensationPolicy.MaxEntityNamesInSummary} more"
                : "";

            summary = $"[×{count} in {displaySeconds}s] {typeName} ({string.Join(", ", topNames)}{suffix})";
        }
        else
        {
            // No non-Actor entity names available: fall back to the first event's message.
            summary = $"[×{count} in {displaySeconds}s] {first.Message}";
        }

        // Build condensation metadata JSON.
        var condensationMeta = new Dictionary<string, object?>
        {
            ["condensed"] = true,
            ["count"] = count,
            ["window_seconds"] = Math.Round(windowSeconds, 1),
            ["first_at"] = first.OccurredAt.ToString("O"),
            ["last_at"] = last.OccurredAt.ToString("O"),
            ["log_type"] = typeName,
            ["sample_messages"] = sampleMessages,
        };

        var json = JsonSerializer.SerializeToDocument(condensationMeta, _jsonOptions);

        return new AdminLogEventWriteData
        {
            ServerId = first.ServerId,
            ServerName = first.ServerName,
            RoundId = first.RoundId,
            Type = type,  // Preserve original type — no burst rewriting.
            Impact = maxImpact,  // Use max impact across the group.
            OccurredAt = first.OccurredAt,
            Message = summary,
            Json = json,
            Players = allPlayers,
            Entities = allEntities,
            PlayerRoles = allPlayerRoles,
        };
    }

    private async Task EnsureServerIdentity()
    {
        if (_serverId > 0)
            return;

        var server = await _serverDbEntry.ServerEntity;
        _serverId = server.Id;
        _serverName = server.Name;
    }

    public void OpenEui(ICommonSession admin, string? search = null, Guid? targetPlayer = null)
    {
        var ui = new AdminLogsEui();
        _euis.OpenEui(ui, admin);

        List<Guid>? userList = null;
        if (targetPlayer is not null)
            userList = [targetPlayer.Value];

        ui.SetLogFilter(search, userList);
    }
}
