using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;

namespace Content.Server.Database;

/// <summary>
/// Implementation of <see cref="IAuditLogManager"/> for recording round-independent administrative actions.
/// </summary>
public sealed class AuditLogManager : IAuditLogManager, IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private Task<int>? _serverIdTask;
    private ISawmill _sawmill = default!;

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("audit");
    }

    private Task<int> GetServerId()
    {
        return _serverIdTask ??= GetServerIdInternal();
    }

    private async Task<int> GetServerIdInternal()
    {
        var name = _cfg.GetCVar(CCVars.AdminLogsServerName);
        var server = await _db.AddOrGetServer(name);
        return server.Id;
    }

    public void Add(
        Guid? adminUserId,
        AuditLogAction actionType,
        string message,
        object jsonData,
        Guid? targetUserId = null,
        string? targetEntityType = null,
        string? targetEntityId = null)
    {
        // Queue the log to be saved asynchronously using TaskManager to maintain IoC context
        _taskManager.RunOnMainThread(async () =>
        {
            try
            {
                await AddInternal(
                    adminUserId,
                    actionType,
                    message,
                    jsonData,
                    targetUserId,
                    targetEntityType,
                    targetEntityId);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - we don't want audit logging failures to break game logic
                _sawmill.Error($"Failed to save audit log: {ex}");
            }
        });
    }

    private async Task AddInternal(
        Guid? adminUserId,
        AuditLogAction actionType,
        string message,
        object jsonData,
        Guid? targetUserId,
        string? targetEntityType,
        string? targetEntityId)
    {
        var serverId = await GetServerId();
        var jsonDocument = JsonSerializer.SerializeToDocument(jsonData);

        var auditLog = new Content.Server.Database.AuditLog
        {
            Timestamp = DateTime.UtcNow,
            AdminUserId = adminUserId,
            ActionType = actionType,
            TargetUserId = targetUserId,
            TargetEntityType = targetEntityType,
            TargetEntityId = targetEntityId,
            Message = message,
            JsonData = jsonDocument,
            ServerId = serverId
        };

        await _db.SaveAuditLog(auditLog);
    }

    public Task<List<Content.Server.Database.AuditLog>> GetLogs(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        return _db.GetAuditLogs(filter, cancellationToken);
    }

    public Task<List<Content.Server.Database.AuditLog>> GetLogsForEntity(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        return _db.GetAuditLogs(new AuditLogFilter
        {
            TargetEntityType = entityType,
            TargetEntityId = entityId
        }, cancellationToken);
    }

    public Task<List<Content.Server.Database.AuditLog>> GetLogsByAdmin(
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        return _db.GetAuditLogs(new AuditLogFilter
        {
            AdminUserId = adminUserId
        }, cancellationToken);
    }

    public Task<List<Content.Server.Database.AuditLog>> GetLogsByTargetUser(
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        return _db.GetAuditLogs(new AuditLogFilter
        {
            TargetUserId = targetUserId
        }, cancellationToken);
    }
}
