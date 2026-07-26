using Content.Shared.Administration.Logs.Payloads;

namespace Content.Server.Administration.AuditLog.Payloads;

/// <summary>
/// Payload for admin entity spawn and delete audit events
/// (<c>AdminAuditAction.SpawnEntity</c>, <c>AdminAuditAction.DeleteEntity</c>).
/// </summary>
/// <param name="AdminId">GUID of the admin who spawned or deleted the entity.</param>
/// <param name="Action">Action taken: <c>"Create"</c> or <c>"Erase"</c>.</param>
/// <param name="EntityPrototype">Prototype ID of the entity. Null if unavailable.</param>
/// <param name="CoordX">World X coordinate of the entity at action time.</param>
/// <param name="CoordY">World Y coordinate of the entity at action time.</param>
public sealed record AuditEntityActionPayload(
    Guid AdminId,
    string Action,
    string? EntityPrototype,
    float? CoordX = null,
    float? CoordY = null) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
