using Content.Shared.Administration.Logs.Payloads;

namespace Content.Server.Administration.AuditLog.Payloads;

/// <summary>
/// Payload for player freeze/unfreeze/mute audit events
/// (<c>AdminAuditAction.Freeze</c>, <c>AdminAuditAction.Unfreeze</c>).
/// </summary>
/// <param name="AdminId">GUID of the admin who issued the freeze/unfreeze.</param>
/// <param name="Action">Action taken: <c>"freeze"</c>, <c>"freeze_and_mute"</c>, or <c>"unfreeze"</c>.</param>
/// <param name="Muted">Whether the player was also muted. False for unfreeze.</param>
public sealed record AuditFreezePayload(
    Guid AdminId,
    string Action,
    bool Muted = false) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 2;
}
