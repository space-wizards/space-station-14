using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Notes;

[Serializable, NetSerializable]
public sealed record SharedAdminNote(
    int Id, // Id of note, message, watchlist, ban or role ban. Should be paired with NoteType to uniquely identify a shared admin note.
    NetUserId Player, // Notes player
    int? Round, // Which round was it added in?
    string? ServerName, // Which server was this added on?
    TimeSpan PlaytimeAtNote, // Playtime at the time of getting the note
    NoteType NoteType, // Type of note
    string Message, // Attached message
    NoteSeverity? NoteSeverity, // Severity of the note, ban or role ban. Otherwise null.
    bool Secret, // Is it visible to the player (only relevant if players can see their own notes)
    string CreatedByName, // Who created it?
    string EditedByName, // Who edited it last?
    DateTime CreatedAt, // When was it created?
    DateTime? LastEditedAt, // When was it last edited?
    DateTime? ExpiryTime, // Does it expire?
    string[]? BannedRoles, // Only valid for role bans. List of banned roles
    DateTime? UnbannedTime, // Only valid for bans. Set if unbanned
    string? UnbannedByName, // Only valid for bans. Set if unbanned
    bool? Seen // Only valid for messages, otherwise should be null. Has the user seen this message?
    );
