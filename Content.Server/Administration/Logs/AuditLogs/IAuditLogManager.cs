using System.Threading.Tasks;
using Content.Shared.Database;

namespace Content.Server.Administration.Logs.AuditLogs;

public interface IAuditLogManager
{
    void Initialize();

    /// <summary>
    /// Adds an audit log to the database.
    /// </summary>
    /// <param name="ty">The type of the log being created.</param>
    /// <param name="impact">The impact level of the log.</param>
    /// <param name="author">The person responsible for creating the log. (e.g. the banning admin for a ban)</param>
    /// <param name="message">Message describing the logged action.</param>
    /// <param name="effected">What players were effected by the log (e.g. the banned player in a ban). This is used for searchability.</param>
    void AddLog(AuditLogType ty, LogImpact impact, Guid author, string message, List<Guid>? effected = null);

    Task AddLogAsync(AuditLogType ty, LogImpact impact, Guid author, string message, List<Guid>? effected = null);
}

