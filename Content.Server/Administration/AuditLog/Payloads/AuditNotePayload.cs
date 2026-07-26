using Content.Shared.Administration.Logs.Payloads;

namespace Content.Server.Administration.AuditLog.Payloads;

/// <summary>
/// Payload for admin note create, edit, and delete audit events
/// (<c>AdminAuditAction.NoteCreate</c>, <c>AdminAuditAction.NoteEdit</c>,
/// <c>AdminAuditAction.NoteDelete</c>, <c>AdminAuditAction.WatchlistCreate</c>,
/// <c>AdminAuditAction.WatchlistEdit</c>, <c>AdminAuditAction.WatchlistDelete</c>).
/// </summary>
/// <param name="AdminId">GUID of the admin who created, edited, or deleted the note.</param>
/// <param name="NoteType">Type of note record: <c>"Note"</c>, <c>"Watchlist"</c>, or <c>"Warning"</c>.</param>
/// <param name="NoteId">Database ID of the note record.</param>
/// <param name="NoteText"></param>
/// <param name="IsSecret">Whether the note is hidden from the player</param>
/// <param name="OldNoteText"></param>
/// <param name="OldSeverity"></param>
/// <param name="ExpiryMinutes">Minutes until the note expires, or null for permanent notes.</param>
public sealed record AuditNotePayload(
    Guid AdminId,
    string NoteType,
    int NoteId,
    string? NoteText,
    bool IsSecret,
    string? OldNoteText = null,
    string? OldSeverity = null,
    int? ExpiryMinutes = null) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}
