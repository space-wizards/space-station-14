using System.Text.Json;
using Content.Shared.Database;

namespace Content.Server.Administration.AuditLog;

public interface IAdminAuditLogManager
{
    void Initialize();
    void Shutdown();
    void Update();
    void RoundStarting(int roundId);

    void LogAction(
        Guid adminUserId,
        AdminAuditAction action,
        AuditSeverity severity,
        string message,
        Guid? targetPlayerUserId = null,
        EntityUid? targetEntity = null,
        JsonDocument? payload = null);
}
