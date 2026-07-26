using Content.Shared.Administration.Logs.Payloads;

namespace Content.Server.Administration.AuditLog.Payloads;

/// <summary>
/// Payload for server ban and role ban audit events (<c>AdminAuditAction.Ban</c>,
/// <c>AdminAuditAction.RoleBan</c>).
/// </summary>
/// <param name="AdminId">GUID of the admin who issued the ban.</param>
/// <param name="BanType">Type of ban: <c>"Server"</c> or <c>"Role"</c>.</param>
/// <param name="TargetName">Display name snapshot of the banned player.</param>
/// <param name="Reason">Ban reason.</param>
/// <param name="DurationMinutes">Ban duration in minutes, or null for a permanent ban.</param>
/// <param name="Expires">UTC expiry timestamp, or null for permanent bans.</param>
/// <param name="Severity">Ban severity string.</param>
/// <param name="Roles">Role prototype IDs for role bans. Null for server bans.</param>
public sealed record AuditBanPayload(
    Guid AdminId,
    string BanType,
    string? TargetName,
    string Reason,
    int? DurationMinutes,
    DateTime? Expires,
    string? Severity = null,
    IReadOnlyList<string>? Roles = null) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
