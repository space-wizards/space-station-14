using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Configuration;

namespace Content.Server.Administration.Logs.AuditLogs;

public sealed class AuditLogManager : IAuditLogManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    private bool _enabled;

    public void Initialize()
    {
        _cfg.OnValueChanged(CCVars.AuditLogsEnabled, newVal => _enabled = newVal, true);
    }

    public async Task AddLogAsync(AuditLogType ty, LogImpact impact, Guid? author, string message, List<Guid>? effected = null)
    {
        if (_enabled)
            await _db.AddAuditLogAsync(ty, impact, author, message, effected);
    }
}
