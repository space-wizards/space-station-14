using System.Collections.Immutable;
using System.Net;
using Content.Shared.Database;
using Robust.Shared.Network;

namespace Content.Server.Database;

// This file contains copies of records returned from the database.
// We can't return the raw EF Core entities as they are often unsuited.
// (e.g. datetime handling of Microsoft.Data.Sqlite)

public interface IAdminRemarksRecord
{
    public int Id { get; }

    public RoundRecord? Round { get; }

    public PlayerRecord? Player { get; }
    public TimeSpan PlaytimeAtNote { get; }

    public string Message { get; }

    public PlayerRecord? CreatedBy { get; }

    public DateTimeOffset CreatedAt { get; }

    public PlayerRecord? LastEditedBy { get; }

    public DateTimeOffset? LastEditedAt { get; }
    public DateTimeOffset? ExpirationTime { get; }

    public bool Deleted { get; }
}

public sealed record ServerRoleBanNoteRecord(
    int Id,
    RoundRecord? Round,
    PlayerRecord? Player,
    TimeSpan PlaytimeAtNote,
    string Message,
    NoteSeverity Severity,
    PlayerRecord? CreatedBy,
    DateTimeOffset CreatedAt,
    PlayerRecord? LastEditedBy,
    DateTimeOffset? LastEditedAt,
    DateTimeOffset? ExpirationTime,
    bool Deleted,
    string[] Roles,
    PlayerRecord? UnbanningAdmin,
    DateTime? UnbanTime) : IAdminRemarksRecord;

public sealed record ServerBanNoteRecord(
    int Id,
    RoundRecord? Round,
    PlayerRecord? Player,
    TimeSpan PlaytimeAtNote,
    string Message,
    NoteSeverity Severity,
    PlayerRecord? CreatedBy,
    DateTimeOffset CreatedAt,
    PlayerRecord? LastEditedBy,
    DateTimeOffset? LastEditedAt,
    DateTimeOffset? ExpirationTime,
    bool Deleted,
    PlayerRecord? UnbanningAdmin,
    DateTime? UnbanTime) : IAdminRemarksRecord;

public sealed record AdminNoteRecord(
    int Id,
    RoundRecord? Round,
    PlayerRecord? Player,
    TimeSpan PlaytimeAtNote,
    string Message,
    NoteSeverity Severity,
    PlayerRecord? CreatedBy,
    DateTimeOffset CreatedAt,
    PlayerRecord? LastEditedBy,
    DateTimeOffset? LastEditedAt,
    DateTimeOffset? ExpirationTime,
    bool Deleted,
    PlayerRecord? DeletedBy,
    DateTimeOffset? DeletedAt,
    bool Secret) : IAdminRemarksRecord;

public sealed record AdminWatchlistRecord(
    int Id,
    RoundRecord? Round,
    PlayerRecord? Player,
    TimeSpan PlaytimeAtNote,
    string Message,
    PlayerRecord? CreatedBy,
    DateTimeOffset CreatedAt,
    PlayerRecord? LastEditedBy,
    DateTimeOffset? LastEditedAt,
    DateTimeOffset? ExpirationTime,
    bool Deleted,
    PlayerRecord? DeletedBy,
    DateTimeOffset? DeletedAt) : IAdminRemarksRecord;

public sealed record AdminMessageRecord(
    int Id,
    RoundRecord? Round,
    PlayerRecord? Player,
    TimeSpan PlaytimeAtNote,
    string Message,
    PlayerRecord? CreatedBy,
    DateTimeOffset CreatedAt,
    PlayerRecord? LastEditedBy,
    DateTimeOffset? LastEditedAt,
    DateTimeOffset? ExpirationTime,
    bool Deleted,
    PlayerRecord? DeletedBy,
    DateTimeOffset? DeletedAt,
    bool Seen,
    bool Dismissed) : IAdminRemarksRecord;


public sealed record PlayerRecord(
    NetUserId UserId,
    DateTimeOffset FirstSeenTime,
    string LastSeenUserName,
    DateTimeOffset LastSeenTime,
    IPAddress LastSeenAddress,
    ImmutableArray<byte>? HWId);

public sealed record RoundRecord(int Id, DateTimeOffset StartDate, ServerRecord Server);

public sealed record ServerRecord(int Id, string Name);
