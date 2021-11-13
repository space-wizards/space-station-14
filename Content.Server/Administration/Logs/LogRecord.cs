using System;

namespace Content.Server.Administration.Logs;

public readonly record struct LogRecord(
    int Id,
    int RoundId,
    string Type,
    DateTime Date,
    string Message
);
