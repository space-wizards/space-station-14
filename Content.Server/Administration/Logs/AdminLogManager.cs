using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Systems;
using Content.Server.Database;
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

            _sawmill.Debug($"Dropping {droppedPreRound} pre-round logs during lobby. Cap: {_preRoundQueueMax}");
        }
        else
        {
            while (_preRoundLogQueue.TryDequeue(out var preRoundLog))
            {
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
                    log.RoundId = _currentRoundId;
                    continue;
                }

                _sawmill.Warning($"Dropping admin log with unresolved round id. Type: {log.Type}, Message: {log.Message}");
                copy.RemoveAt(i);
            }

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

    public async void RoundStarting(int id)
    {
        _currentRoundId = id;

        // Flush pre-round logs immediately now that we have a valid round ID.
        // Use dropPreRoundInLobby: false because the round is starting — these
        // logs should be persisted, not dropped. Bypass TrySaveLogs guard since
        // this is a lifecycle event that must flush before the round proceeds.
        if (!_preRoundLogQueue.IsEmpty || !_logQueue.IsEmpty)
        {
            // Wait for any in-progress save to complete
            while (Interlocked.CompareExchange(ref _savingLogs, 1, 0) != 0)
                await Task.Yield();

            try
            {
                await SaveLogs(dropPreRoundInLobby: false);
            }
            finally
            {
                Interlocked.Exchange(ref _savingLogs, 0);
            }
        }
    }

    public void RunLevelChanged(GameRunLevel level)
    {
        _runLevel = level;

        if (level == GameRunLevel.PreRoundLobby)
        {
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

    public override void Add(
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
            // Skip auto-extracted entities whose UID already has an explicit entry.
            var alreadyExplicit = false;
            foreach (var existing in logEntities)
            {
                if (existing.EntityUid == autoEntity.EntityUid)
                {
                    alreadyExplicit = true;
                    break;
                }
            }

            if (!alreadyExplicit)
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

            //stripping
            case LogType.Stripping:
                if (ContainsAny(key, "actor", "user", "player"))
                    return AdminLogEntityRole.Actor;
                if (ContainsAny(key, "victim", "target"))
                    return AdminLogEntityRole.Victim;
                if (ContainsAny(key, "subject", "item"))
                    return AdminLogEntityRole.Subject;
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
            outString += $"[cmdlink=\"{EscapeText(player.CharacterName)}\" command=\"tpto {player.NetEnt}\"/]";

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

    /// <summary>
    /// Escape the given text to not allow breakouts of the cmdlink tags.
    /// </summary>
    private string EscapeText(string text)
    {
        return FormattedMessage.EscapeText(text).Replace("\"", "\\\"").Replace("'", "\\'");
    }

    public async Task<List<SharedAdminLog>> All(LogFilter? filter = null, Func<List<SharedAdminLog>>? listProvider = null)
    {
        if (_serverId <= 0)
            await EnsureServerIdentity();

        filter = ApplyServerScope(filter);

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

    private async Task EnsureServerIdentity()
    {
        if (_serverId > 0)
            return;

        var server = await _serverDbEntry.ServerEntity;
        _serverId = server.Id;
        _serverName = server.Name;
    }
}
