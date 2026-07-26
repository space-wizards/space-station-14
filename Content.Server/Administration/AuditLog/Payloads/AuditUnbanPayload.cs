using Content.Shared.Administration.Logs.Payloads;

namespace Content.Server.Administration.AuditLog.Payloads;

/// <summary>
/// Payload for ban pardon audit events (<c>AdminAuditAction.Unban</c>,
/// <c>AdminAuditAction.RoleUnban</c>).
/// </summary>
/// <param name="AdminId">GUID of the admin who issued the pardon.</param>
/// <param name="BanId">The ID of the ban that was pardoned.</param>
/// <param name="BanType">Type of the pardoned ban: <c>"Server"</c> or <c>"Role"</c>.</param>
/// <param name="OriginalReason">Original ban reason.</param>
/// <param name="Roles">Role prototype IDs for role ban pardons. Null for server ban pardons.</param>
public sealed record AuditUnbanPayload(
    Guid AdminId,
    int BanId,
    string BanType,
    string? OriginalReason = null,
    IReadOnlyList<string?>? Roles = null) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
