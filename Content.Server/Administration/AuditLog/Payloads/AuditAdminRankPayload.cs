using Content.Shared.Administration.Logs.Payloads;

namespace Content.Server.Administration.AuditLog.Payloads;

/// <summary>
/// Payload for admin self-rank-change events:
/// <c>AdminAuditAction.DeAdmin</c>, <c>AdminAuditAction.ReAdmin</c>.
/// </summary>
/// <param name="AdminId">GUID of the admin</param>
/// <param name="Action">Action taken: <c>"deadmin_self"</c> or <c>"readmin_self"</c>.</param>
/// <param name="NewRank">The admin rank name after the change. Null for de-admin.</param>
public sealed record AuditAdminRankPayload(
    Guid AdminId,
    string Action,
    string? NewRank = null) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
