using System;
using System.Collections.Immutable;
using Content.Server.Database;

namespace Content.Server.Administration.Logs;

public sealed record LogRecord<T>(
    int Id,
    DateTime Date,
    ImmutableList<PlayerRecord> Players,
    int RoundId,
    T Log,
    string Message
);
