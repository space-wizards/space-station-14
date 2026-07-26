using Content.Shared.Administration.Logs.Payloads;

namespace Content.Server.Administration.AuditLog.Payloads;

/// <summary>
/// Payload for CVar change audit events (<c>AdminAuditAction.CvarChange</c>).
/// </summary>
/// <param name="AdminId">GUID of the admin who changed the CVar.</param>
/// <param name="CvarName">The CVar name that was changed.</param>
/// <param name="OldValue">String representation of the previous value.</param>
/// <param name="NewValue">String representation of the new value.</param>
public sealed record AuditCvarPayload(
    Guid AdminId,
    string CvarName,
    string OldValue,
    string NewValue) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
