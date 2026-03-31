using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
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
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;

    private const string SawmillId = "admin.audit_logs";

    private ISawmill _sawmill = default!;
    private readonly ConcurrentQueue<AdminAuditEventWriteData> _logQueue = new();

    private bool _enabled;
    private TimeSpan _queueSendDelay;
    private TimeSpan _nextUpdateTime;
    private int _savingLogs;
    private int _serverId;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);

        _configuration.OnValueChanged(CCVars.AdminAuditLogEnabled,
            value => _enabled = value, true);
        _configuration.OnValueChanged(CCVars.AdminAuditLogQueueSendDelay,
            value => _queueSendDelay = TimeSpan.FromSeconds(value), true);

        _consoleHost.AnyCommandExecuted += OnAnyCommandExecuted;

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
            return;

        if (!_adminManager.IsAdmin(session))
            return;

        LogAction(
            session.UserId,
            AdminAuditAction.CommandExecution,
            AuditSeverity.Routine,
            $"Executed command: {argStr}");
    }

    public void Update()
    {
        if (!_enabled || _logQueue.IsEmpty)
            return;

        if (_timing.RealTime < _nextUpdateTime)
            return;

        _ = TryFlushLogs();
    }

    public void RoundStarting(int roundId)
    {
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

        var roundId = _gameTicker.RoundId;

        _logQueue.Enqueue(new AdminAuditEventWriteData
        {
            ServerId = _serverId,
            RoundId = roundId > 0 ? roundId : null,
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
        });
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
