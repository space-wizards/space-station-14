using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.Administration.AuditLog;

public sealed class AdminAuditLogManager : IAdminAuditLogManager
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ServerDbEntryManager _serverDbEntry = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IViewVariablesManager _vvManager = default!;

    private const string SawmillId = "admin.audit_logs";

    private static readonly HashSet<string> SensitiveCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "setpassword",
        "changepassword",
        "loadconfigfile",
        "saveconfig",
    };

    private static readonly HashSet<string> RedactedArgCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        //this can be used to redact stuff if needed in the audit log
    };

    //List of Commands to not log, note these are mostly things that are either already logged, or not important
    private static readonly HashSet<string> ExcludedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "say",
        "ooc",
        "looc",
        "dsay",
        "me",
        "whisper",
        "tsay",
        "radio",
        "toggleready",
        "joinround",
    };

    private ISawmill _sawmill = default!;
    private readonly ConcurrentQueue<AdminAuditEventWriteData> _logQueue = new();
    private readonly ConcurrentQueue<AdminAuditEventWriteData> _preRoundQueue = new();

    private bool _enabled;
    private TimeSpan _queueSendDelay;
    private TimeSpan _nextUpdateTime;
    private int _savingLogs;
    private int _serverId;
    private int _roundId;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);

        _configuration.OnValueChanged(CCVars.AdminAuditLogEnabled,
            value => _enabled = value, true);
        _configuration.OnValueChanged(CCVars.AdminAuditLogQueueSendDelay,
            value => _queueSendDelay = TimeSpan.FromSeconds(value), true);

        _consoleHost.AnyCommandExecuted += OnAnyCommandExecuted;
        _vvManager.PropertyModified += OnVVPropertyModified;

        _ = Task.Run(async () =>
        {
            try
            {
                await EnsureServerIdentity();
            }
            catch (Exception e)
            {
                _sawmill.Warning($"Failed to resolve audit-log server identity during initialization: {e}");
            }
        });
    }

    public void Shutdown()
    {
        _consoleHost.AnyCommandExecuted -= OnAnyCommandExecuted;
        _vvManager.PropertyModified -= OnVVPropertyModified;

        // Drain pre-round queue into main queue on shutdown so they're not lost.
        while (_preRoundQueue.TryDequeue(out var log))
            _logQueue.Enqueue(log);

        if (_logQueue.IsEmpty)
            return;

        try
        {
            FlushLogs().GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to flush audit logs during shutdown: {e}");
        }
    }

    private void OnAnyCommandExecuted(IConsoleShell shell, string commandName, string argStr, string[] args)
    {
        if (shell.Player is not { } session)
        {
            _sawmill.Verbose($"AnyCommandExecuted fired for '{commandName}' but shell.Player is null (server console).");
            return;
        }

        if (!_adminManager.IsAdmin(session))
            return;

        if (ExcludedCommands.Contains(commandName))
            return;

        var logMessage = argStr;
        if (SensitiveCommands.Contains(commandName))
            logMessage = $"{commandName} [arguments redacted]";
        else if (RedactedArgCommands.Contains(commandName))
            logMessage = $"{commandName} [...]";

        _sawmill.Debug($"Audit logging command: {session.Name} executed '{commandName}'");

        LogAction(
            session.UserId,
            AdminAuditAction.CommandExecution,
            AuditSeverity.Routine,
            $"{session.Name} executed: {logMessage}");
    }

    private void OnVVPropertyModified(NetUserId userId, object target, string memberName, object? oldValue, object? newValue)
    {
        var oldStr = FormatVVValue(oldValue);
        var newStr = FormatVVValue(newValue);

        // If the target is an entity component, we want the enitity
        EntityUid? targetEntity = null;
        string entityDesc = "";
        var componentName = memberName;

        if (target is IComponent comp)
        {
            //Constuct a nice string with the comp name
            componentName = $"{comp.GetType().Name}.{memberName.Split('.').LastOrDefault() ?? memberName}";

            if (comp.Owner.IsValid())
            {
                targetEntity = comp.Owner;
                entityDesc = $" on {_entityManager.ToPrettyString(comp.Owner)}";
            }
        }

        LogAction(
            userId.UserId,
            AdminAuditAction.VVWrite,
            AuditSeverity.Notable,
            $"VV modified {componentName}{entityDesc}: {oldStr} → {newStr}",
            targetEntity: targetEntity,
            payload: JsonSerializer.SerializeToDocument(new
            {
                component = target is IComponent c ? c.GetType().Name : target.GetType().Name,
                member = memberName,
                oldValue = oldStr,
                newValue = newStr
            }));
    }

    //There are too many Vs in this name
    private static string FormatVVValue(object? value)
    {
        if (value == null)
            return "<null>";

        // For collections make a list
        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            var items = new List<string>();
            foreach (var item in enumerable)
                items.Add(item?.ToString() ?? "<null>");

            return items.Count == 0 ? "[]" : $"[{string.Join(", ", items)}]";
        }

        return value.ToString() ?? "<null>";
    }

    public void Update()
    {
        if (!_enabled || _logQueue.IsEmpty)
            return;

        if (_timing.RealTime < _nextUpdateTime)
            return;

        _ = TryFlushLogs();
    }

    public async void RoundStarting(int roundId)
    {
        _roundId = roundId;

        // Assign pre-round audit logs to this round and flush them immediately.
        // This mirrors how admin logs handle pre-round attribution.
        while (_preRoundQueue.TryDequeue(out var log))
        {
            log.RoundId = roundId;
            _logQueue.Enqueue(log);
        }

        if (!_logQueue.IsEmpty)
            await TryFlushLogs();
    }

    public void LogAction(
        Guid adminUserId,
        AdminAuditAction action,
        AuditSeverity severity,
        string message,
        Guid? targetPlayerUserId = null,
        EntityUid? targetEntity = null,
        JsonDocument? payload = null)
    {
        if (!_enabled)
            return;

        int? targetEntityUid = null;
        string? targetEntityName = null;
        string? targetEntityPrototype = null;

        if (targetEntity != null)
        {
            targetEntityUid = (int) targetEntity.Value;

            if (_entityManager.TryGetComponent(targetEntity.Value, out MetaDataComponent? metadata))
            {
                targetEntityName = metadata.EntityName;
                targetEntityPrototype = metadata.EntityPrototype?.ID;
            }
        }

        if (message.Contains('\0'))
            message = message.Replace("\0", "");

        var roundId = _roundId;
        var isPreRound = roundId <= 0;

        var writeData = new AdminAuditEventWriteData
        {
            ServerId = _serverId,
            RoundId = isPreRound ? null : roundId,
            AdminUserId = adminUserId,
            Action = action,
            Severity = severity,
            OccurredAt = DateTime.UtcNow,
            Message = message,
            TargetPlayerUserId = targetPlayerUserId,
            TargetEntityUid = targetEntityUid,
            TargetEntityName = targetEntityName,
            TargetEntityPrototype = targetEntityPrototype,
            Json = payload,
        };

        // Buffer pre-round logs until a round starts so they get assigned the correct
        // round ID. This prevents NULL-round rows from leaking into every round query.
        if (isPreRound)
            _preRoundQueue.Enqueue(writeData);
        else
            _logQueue.Enqueue(writeData);
    }

    private async Task TryFlushLogs()
    {
        if (Interlocked.Exchange(ref _savingLogs, 1) == 1)
            return;

        try
        {
            await FlushLogs();
        }
        finally
        {
            Interlocked.Exchange(ref _savingLogs, 0);
        }
    }

    private async Task FlushLogs()
    {
        _nextUpdateTime = _timing.RealTime.Add(_queueSendDelay);

        var count = _logQueue.Count;
        if (count == 0)
            return;

        var copy = new List<AdminAuditEventWriteData>(count);
        while (_logQueue.TryDequeue(out var log))
        {
            copy.Add(log);
        }

        try
        {
            await EnsureServerIdentity();

            for (var i = 0; i < copy.Count; i++)
            {
                if (copy[i].ServerId <= 0)
                    copy[i].ServerId = _serverId;
            }

            if (copy.Count == 0)
                return;

            await _db.AddAuditLogs(copy);
            _sawmill.Debug($"Saved {copy.Count} admin audit logs.");
        }
        catch (Exception e)
        {
            foreach (var log in copy)
            {
                _logQueue.Enqueue(log);
            }

            _sawmill.Error($"Failed to persist admin audit logs. Re-queued {copy.Count} logs. Error: {e}");
        }
    }

    private async Task EnsureServerIdentity()
    {
        if (_serverId > 0)
            return;

        var server = await _serverDbEntry.ServerEntity;
        _serverId = server.Id;
    }
}
