using Content.Shared.Administration.Logs.Payloads;

namespace Content.Server.Administration.AuditLog.Payloads;

/// <summary>
/// Payload for round management audit actions:
/// <c>AdminAuditAction.CallShuttle</c>, <c>AdminAuditAction.RecallShuttle</c>,
/// <c>AdminAuditAction.RestartRound</c>, <c>AdminAuditAction.EndRound</c>,
/// <c>AdminAuditAction.StartRound</c>, <c>AdminAuditAction.ForcePreset</c>,
/// <c>AdminAuditAction.ForceMap</c>.
/// </summary>
/// <param name="AdminId">GUID of the admin who performed the round action.</param>
/// <param name="Action">The specific action taken, matching the <c>AdminAuditAction</c> string name.</param>
/// <param name="Reason">Admin-provided reason. Null if not provided.</param>
/// <param name="PresetId">Forced preset prototype ID, when applicable. Null otherwise.</param>
/// <param name="MapId">Forced map prototype ID, when applicable. Null otherwise.</param>
public sealed record AuditRoundActionPayload(
    Guid AdminId,
    string Action,
    string? Reason = null,
    string? PresetId = null,
    string? MapId = null) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
