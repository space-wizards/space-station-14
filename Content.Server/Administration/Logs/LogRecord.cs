using System;
using Content.Server.Database;
using Content.Shared.Administration.Logs;

namespace Content.Server.Administration.Logs;

public class LogRecord
{
    public LogRecord(
        int id,
        int roundId,
        LogType type,
        DateTime date,
        string message)
    {
        Id = id;
        RoundId = roundId;
        Type = type;
        Date = date;
        Message = message;
    }

    public int Id { get; }
    public int RoundId { get; }
    public LogType Type { get; }
    public DateTime Date { get; }
    public string Message { get; }
}
