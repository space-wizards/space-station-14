using Content.Shared.Administration.Logs.Payloads;

namespace Content.Server.Administration.AuditLog.Payloads;

/// <summary>
/// Payload for admin permissions change events managed through
/// <c>PermissionsEui</c> or equivalent:
/// <c>AdminAuditAction.AdminRankChange</c>, promote/demote, title changes, etc.
/// </summary>
/// <param name="AdminId">GUID of the admin making the permissions change.</param>
/// <param name="Action">Human-readable action description, e.g. <c>"grant_rank"</c>, <c>"revoke_rank"</c>.</param>
/// <param name="OldRankName">Previous admin rank name. Null if the target was not an admin before.</param>
/// <param name="NewRankName">New admin rank name. Null if the target was de-admined.</param>
/// <param name="GrantedFlags">Bitmask representation of granted admin flags.</param>
/// <param name="RevokedFlags">Bitmask representation of revoked admin flags.</param>
public sealed record AuditPermissionsPayload(
    Guid AdminId,
    string Action,
    string? OldRankName = null,
    string? NewRankName = null,
    uint? GrantedFlags = null,
    uint? RevokedFlags = null) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
