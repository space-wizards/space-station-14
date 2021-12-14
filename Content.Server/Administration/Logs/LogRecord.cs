using System;
using System.Collections.Generic;
using Content.Server.Database;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.Administration.Logs;

public class LogRecord
{
    public LogRecord(
        int id,
        int roundId,
        LogType type,
        LogImpact impact,
        DateTime date,
        string message,
        Guid[] players)
    {
        Id = id;
        RoundId = roundId;
        Type = type;
        Impact = impact;
        Date = date;
        Message = message;
        Players = players;
    }

    public int Id { get; }
    public int RoundId { get; }
    public LogType Type { get; }
    public LogImpact Impact { get; }
    public DateTime Date { get; }
    public string Message { get; }
    public Guid[] Players { get; }
}
